using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Taxl;

public sealed class TaxlParser(SourceFileView source)
{
    private const string CodeFragmentStart = "//---";
    private const string OutputFragmentStart = "//===";
    private const string DirectiveStart = "//@";
    private const string AnnotationStart = "//~";
    private const string CommentStart = "//";

    private const string ErrorAnnotation = "error";
    private const string LintAnnotation = "lint";
    private const string TypeAnnotation = "type";


    public ImmutableArray<TaxlFragment> ParseFragments()
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
    
    
    private TaxlFragment ParseFragment(SourceFileView fragmentView)
    {
        var text = fragmentView.TextSpan;
        var isFirstFragment = !text.StartsWith(CodeFragmentStart) && !text.StartsWith(OutputFragmentStart);
        
        Debug.Assert(CodeFragmentStart.Length == OutputFragmentStart.Length);
        var argument = isFirstFragment ? "" : FirstLineText(text)[CodeFragmentStart.Length..].Trim().ToString();
        
        if (isFirstFragment || text.StartsWith(CodeFragmentStart))
            return new TaxlFragment.Code(fragmentView, argument, ParseAnnotations(fragmentView));
        
        Debug.Assert(text.StartsWith(OutputFragmentStart));
        return new TaxlFragment.Output(fragmentView, argument);
    }


    public ImmutableArray<TaxlDirective> ParseDirectives(TaxlFragment fragment)
    {
        // Directives are only accepted at the top of _source and
        // before any other text. Only whitespace and comments are
        // accepted among directives.
        
        var text = fragment.SourceView.TextSpan;
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

    private TaxlDirective ParseDirective(SourceLocation location)
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


    private ImmutableArray<TaxlAnnotation> ParseAnnotations(SourceFileView fragmentView)
    {
        // Run the lexer and only use comment tokens to avoid 
        // mistakenly parsing "//~" inside strings or other comments.

        return
        [
            .. Lexer.Lex(fragmentView, new DiagnosticBag())
                .Where(t => t.Kind is TokenKind.Comment)
                .Where(t => fragmentView.GetText(t.FullSpan).StartsWith(AnnotationStart))
                .Select(t => ParseAnnotation(fragmentView.GetLocation(t.FullSpan)))
        ];
    }


    private TaxlAnnotation ParseAnnotation(SourceLocation location)
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

    private TaxlAnnotation ParseTypeAnnotationTail(SourceLocation annotationLocation, int tailStartIndex)
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

    private SourceSpan? GetCaret(SourceLocation annotationLocation, int tailStartIndex)
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

    private SourceSpan? GetCaretReferencedSpan(SourceLocation annotationLocation, SourceSpan caretSpan)
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
    

    private int NextFragmentIndex(ReadOnlySpan<char> text, int startIndex)
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

    private int NextNewlineOrEnd(ReadOnlySpan<char> text, int startIndex)
    {
        var newlineIndex = text[startIndex..].IndexOf('\n') + startIndex;
        return newlineIndex > startIndex ? newlineIndex : text.Length;
    }
    
    private ReadOnlySpan<char> FirstLineText(ReadOnlySpan<char> text)
    {
        var newlineIndex = text.IndexOf('\n');
        return newlineIndex >= 0
            ? text[..newlineIndex]
            : text;
    }
}