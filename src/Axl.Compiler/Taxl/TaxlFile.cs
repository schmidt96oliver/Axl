using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Taxl;

public sealed class TaxlFile
{
    private const string CodeFragmentStart = "//---";
    private const string OutputFragmentStart = "//===";
    private const string DirectiveStart = "//@";
    private const string CommentStart = "//";
    
    private const string AnnotationStart = "//~";
    private const string ErrorAnnotation = "error";
    private const string LintAnnotation = "lint";
    private const string TypeAnnotation = "type";

    
    public SourceFileView Source { get; }
    public ImmutableArray<TaxlFragment> Fragments { get; }
    public ImmutableArray<TaxlDirective> Directives { get; }


    private TaxlFile(SourceFileView source, ImmutableArray<TaxlFragment> fragments, ImmutableArray<TaxlDirective> directives)
    {
        Source = source;
        Fragments = fragments;
        Directives = directives;
    }


    public static TaxlFile Parse(SourceFileView source)
    {
        var fragments = ParseFragments(source);
        var directives = ParseDirectives(fragments[0].SourceView);
        return new TaxlFile(source, fragments, directives);
    }

    private static ImmutableArray<TaxlFragment> ParseFragments(SourceFileView source)
    {
        var text = source.TextSpan;
        var fragments = ImmutableArray.CreateBuilder<TaxlFragment>();

        var fragmentStart = 0;

        while (fragmentStart < text.Length)
        {
            var nextFragmentStart = NextFragmentIndex(text, fragmentStart + 1);
            var fragmentEnd = nextFragmentStart >= 0 ? nextFragmentStart : text.Length;

            var fragmentSpan = source.SpanFromTo(fragmentStart, fragmentEnd);
            var fragmentView = new SourceFileView(source.File, fragmentSpan);

            fragments.Add(ParseFragment(fragmentView));
            fragmentStart = fragmentEnd;
        }

        return fragments.Count > 0
            ? fragments.DrainToImmutable()
            : [new TaxlFragment.Code(source, "", Annotations: [])];
    }
    
    
    private static TaxlFragment ParseFragment(SourceFileView source)
    {
        var text = source.TextSpan;
        var isFirstFragment = !text.StartsWith(CodeFragmentStart) && !text.StartsWith(OutputFragmentStart);
        
        Debug.Assert(CodeFragmentStart.Length == OutputFragmentStart.Length);
        var argument = isFirstFragment ? "" : FirstLineText(text)[CodeFragmentStart.Length..].Trim().ToString();
        
        if (isFirstFragment || text.StartsWith(CodeFragmentStart))
            return new TaxlFragment.Code(source, argument, ParseAnnotations(source));
        
        Debug.Assert(text.StartsWith(OutputFragmentStart));
        return new TaxlFragment.Output(source, argument);
    }


    private static ImmutableArray<TaxlDirective> ParseDirectives(SourceFileView source)
    {
        // Directives are only accepted at the top of source and
        // before any other text. Only whitespace and comments are
        // accepted among directives.
        
        var text = source.TextSpan;
        var directives = ImmutableArray.CreateBuilder<TaxlDirective>();

        for (var i = 0; i < text.Length; )
        {
            if (text[i..].StartsWith(DirectiveStart))
            {
                var directiveStart = i;
                var directiveEnd = NextNewlineOrEnd(text, directiveStart);

                var directiveLocation = source.LocationFromTo(directiveStart, directiveEnd);
                directives.Add(ParseDirective(directiveLocation));

                i = directiveEnd;
            }
            else if (text[i..].StartsWith(CommentStart))
                i = NextNewlineOrEnd(text, i);
            else if (char.IsWhiteSpace(text[i]))
                i++;
            else
                break;
        }

        return directives.DrainToImmutable();
    }

    private static TaxlDirective ParseDirective(SourceLocation location)
    {
        var text = location.GetText();
        Debug.Assert(text.StartsWith(DirectiveStart));

        var kindText = text[DirectiveStart.Length..].Trim();

        var kind = kindText switch
        {
            "run-pass" => TaxlDirectiveKind.RunPass,
            "run-panic" => TaxlDirectiveKind.RunPanic,
            "check" => TaxlDirectiveKind.Check,
            _ => TaxlDirectiveKind.Unknown
        };

        return new TaxlDirective(kind, location.Span);
    }


    private static ImmutableArray<TaxlAnnotation> ParseAnnotations(SourceFileView source)
    {
        // Run the lexer and only use comment tokens to avoid 
        // mistakenly parsing "//~" inside strings or other comments.

        return
        [
            .. Lexer.Lex(source, new DiagnosticBag())
                .Where(t => t.Kind is TokenKind.Comment)
                .Where(t => source.GetText(t.FullSpan).StartsWith(AnnotationStart))
                .Select(t => ParseAnnotation(source.GetLocation(t.FullSpan)))
        ];
    }


    private static TaxlAnnotation ParseAnnotation(SourceLocation location)
    {
        var text = location.GetText();
        Debug.Assert(text.StartsWith(AnnotationStart));

        var prefixLength = AnnotationStart.Length;
        
        // Skip whitespace after "//~"
        for (; prefixLength < text.Length; prefixLength++)
        {
            if (!char.IsWhiteSpace(text[prefixLength]))
                break;
        }

        // --- Diagnostic
        var rest = prefixLength < text.Length ? text[prefixLength..] : "";
        if (rest.StartsWith(ErrorAnnotation) || rest.StartsWith(LintAnnotation))
        {
            var kind = rest.StartsWith(ErrorAnnotation) ? DiagnosticKind.Error : DiagnosticKind.Lint;
            
            prefixLength += ErrorAnnotation.Length;
            var tailStartIndex = prefixLength;
            var tail = tailStartIndex < text.Length ? text[tailStartIndex..] : "";
            
            return new TaxlAnnotation.Diagnostic(kind,
                Id: tail.Trim().ToString(),
                LineNumber: location.StartLinePosition.Line,
                AnnotationSpan: location.Span,
                ArgumentSpan: SourceSpan.InsideSourceFile(location.Span.First + tailStartIndex, length: tail.Length));
        }

        // --- Type
        if (rest.StartsWith(TypeAnnotation))
        {
            prefixLength += TypeAnnotation.Length;
            var tailStartIndex = prefixLength;

            return ParseTypeAnnotationTail(location, tailStartIndex);
        }

        return new TaxlAnnotation.Invalid("Unknown annotation.", location.Span);
    }

    private static TaxlAnnotation ParseTypeAnnotationTail(SourceLocation annotationLocation, int tailStartIndex)
    {
        // --- Location reference
        if (GetCaret(annotationLocation, tailStartIndex) is not SourceSpan caretSpan)
            return new TaxlAnnotation.Invalid("Type annotation must use '^' to point at an expression.", annotationLocation.Span);

        if (GetCaretReferencedSpan(annotationLocation, caretSpan) is not SourceSpan caretReferencedSpan)
            return new TaxlAnnotation.Invalid("Caret points to invalid location.", annotationLocation.Span);
        
        // --- Argument
        var typeNameSpan = SourceSpan.InsideSourceFile(caretSpan.End,
            length: annotationLocation.Span.End - caretSpan.End);
        var typeName = typeNameSpan.IsEmpty ? "" : annotationLocation.File.GetText(typeNameSpan).Trim().ToString();
        
        if (typeName.Contains('^'))
        {
            return new TaxlAnnotation.Invalid("Type annotation can only contain one block of carets.",
                annotationLocation.Span);
        }
        
        return new TaxlAnnotation.Type(
            ExprSpan: caretReferencedSpan, 
            TypeName: typeName, 
            AnnotationSpan: annotationLocation.Span,
            ArgumentSpan: typeNameSpan);
    }

    private static SourceSpan? GetCaret(SourceLocation annotationLocation, int tailStartIndex)
    {
        var text = annotationLocation.GetText();
        
        var caretStart = text[tailStartIndex..].IndexOf('^') + tailStartIndex;
        if (caretStart < tailStartIndex)
            return null;
        var caretLength = 1;
        
        for (; caretStart + caretLength < text.Length; caretLength++)
        {
            if (text[caretStart + caretLength] is not '^')
                break;
        }

        return SourceSpan.InsideSourceFile(
            first: annotationLocation.Span.First + caretStart,
            caretLength);
    }

    private static SourceSpan? GetCaretReferencedSpan(SourceLocation annotationLocation, SourceSpan caretSpan)
    {
        var line = annotationLocation.File.GetLineAt(caretSpan.First);
        Debug.Assert(line.Span.Contains(caretSpan), "One annotation is one line.");

        if (line.LineNumber <= 0)
            return null;
        
        var lineAbove = annotationLocation.File.Lines[line.LineNumber - 1];
        var referencedSpan = SourceSpan.InsideSourceFile(
            first: lineAbove.Span.First + caretSpan.First - line.Span.First,
            length: caretSpan.Length);

        if (!lineAbove.Span.Contains(referencedSpan))
            return null;

        return referencedSpan;
    }
    
    // private static TaxlAnnotation ParseTypeAnnotation(SourceLocation location, int afterAnnotationNameIndex)
    // {
    //     var text = location.GetText();
    //     
    //     // Carets relative to text
    //     var caretStartInText = text[afterAnnotationNameIndex..].IndexOf('^') + afterAnnotationNameIndex;
    //     if (caretStartInText < afterAnnotationNameIndex)
    //         return new TaxlAnnotation.Invalid("Type annotation must use '^' to point at an expression.", location.Span);
    //     var caretLength = 1;
    //     
    //     for (; caretStartInText + caretLength < text.Length; caretLength++)
    //     {
    //         if (text[caretStartInText + caretLength] is not '^')
    //             break;
    //     }                         
    //     
    //     // Make relative to file
    //     var caretSpan = SourceSpan.InsideSourceFile(
    //         first: location.Span.First + caretStartInText,
    //         caretLength);
    //     
    //     // Make relative to line
    //     var caretLine = location.File.GetLineAt(caretSpan.First);
    //     Debug.Assert(caretLength <= caretLine.Span.Length, "One annotation is one line.");
    //
    //     var inCaretLineStart = caretSpan.First - caretLine.Span.First;
    //     Debug.Assert(inCaretLineStart >= 0);
    //     
    //     // Retrieve line above
    //     if (caretLine.LineNumber <= 0)
    //     {
    //         return new TaxlAnnotation.Invalid(
    //             "Type annotation with carets on first line. It cannot point to line above.", location.Span);
    //     }
    //
    //     // Make relative to line above
    //     var lineAbove = location.File.Lines[caretLine.LineNumber - 1];
    //     var referencedSpan = SourceSpan.InsideSourceFile(
    //         first: lineAbove.Span.First + inCaretLineStart,
    //         caretLength);
    //     if (!lineAbove.Span.Contains(referencedSpan))
    //     {
    //         return new TaxlAnnotation.Invalid(
    //             "Type annotation does not reference a valid position in line above.", location.Span);
    //     }
    //     
    //     // Get type name
    //     var typeNameIndex = caretStartInText + caretLength;
    //     var typeName = typeNameIndex < text.Length
    //         ? text[typeNameIndex..].Trim().ToString()
    //         : "";
    //     if (typeName.Contains('^'))
    //     {
    //         return new TaxlAnnotation.Invalid("Type annotation can only contain one block of carets.",
    //             location.Span);
    //     }
    //     
    //     return new TaxlAnnotation.Type(referencedSpan, typeName, location.Span,
    //         PrefixAndLocatorSpan: SourceSpan.InsideSourceFile(location.Span.First, 
    //             length: caretSpan.End - location.Span.First));
    // }
    


    private static int NextFragmentIndex(ReadOnlySpan<char> text, int startIndex)
    {
        var rest = text[startIndex..];

        // Only accept fragment separator if there were no other
        // non-whitespace characters on the same line before.
        // This is practical and means that we avoid handling
        // strings and normal comments.
        
        var lineHasContext = false;
        for (var i = 0; i < rest.Length; i++)
        {
            if (!lineHasContext && (rest[i..].StartsWith(CodeFragmentStart) || rest[i..].StartsWith(OutputFragmentStart)))
                return startIndex + i;

            if (rest[i] is '\n')
                lineHasContext = false;
            else if (char.IsWhiteSpace(rest[i]))
                continue;
            else
                lineHasContext = true;
        }

        return -1;
    }

    private static int NextNewlineOrEnd(ReadOnlySpan<char> text, int startIndex)
    {
        var newlineIndex = text[startIndex..].IndexOf('\n') + startIndex;
        return newlineIndex > startIndex ? newlineIndex : text.Length;
    }
    
    private static ReadOnlySpan<char> FirstLineText(ReadOnlySpan<char> text)
    {
        var newlineIndex = text.IndexOf('\n');
        return newlineIndex >= 0
            ? text[..newlineIndex]
            : text;
    }
}