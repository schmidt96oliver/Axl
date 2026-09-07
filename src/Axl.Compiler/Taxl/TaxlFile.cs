using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Taxl;

public sealed class TaxlFile
{
    private const string CodeFragmentSeparator = "//---";
    private const string OutputFragmentSeparator = "//===";
    private const string DirectiveStart = "//@";
    private const string CommentStart = "//";
    
    private const string AnnotationStart = "//~";
    private const string ErrorAnnotation = "error";
    private const string LintAnnotation = "lint";
    private const string TypeAnnotation = "type";

    
    public ImmutableArray<TaxlFragment> Fragments { get; }
    public ImmutableArray<TaxlDirective> Directives { get; }


    private TaxlFile(ImmutableArray<TaxlFragment> fragments, ImmutableArray<TaxlDirective> directives)
    {
        Fragments = fragments;
        Directives = directives;
    }
    
    
    public static TaxlFile Parse(SourceFileView source)
    {
        // Split into Fragments
        var text = source.TextSpan;
        var fragments = ImmutableArray.CreateBuilder<TaxlFragment>();

        var start = 0;

        while (start < source.Span.Length)
        {
            var nextFragmentIndex = NextFragmentIndex(text, start + 1);
            var end = nextFragmentIndex >= 0
                ? nextFragmentIndex
                : source.Span.Length;

            var fragmentSpan = source.SpanFromTo(start, end);
            var fragmentView = new SourceFileView(source.File, fragmentSpan);

            fragments.Add(ParseFragment(fragmentView));
            start = end;
        }

        var directives = ParseDirectives(fragments[0].View);

        return new TaxlFile(fragments.DrainToImmutable(), directives);
    }

    private static TaxlFragment ParseFragment(SourceFileView source)
    {
        if (source.TextSpan.StartsWith(OutputFragmentSeparator))
            return ParseOutputFragment(source);

        return ParseCodeFragment(source);
    }

    private static TaxlFragment.Code ParseCodeFragment(SourceFileView source)
    {
        var text = source.TextSpan;
        var isFirstFragment = !text.StartsWith(CodeFragmentSeparator) && !text.StartsWith(OutputFragmentSeparator);
        
        Debug.Assert(text.StartsWith(CodeFragmentSeparator) || isFirstFragment);

        var name = isFirstFragment ? "" : FirstLineText(text)[CodeFragmentSeparator.Length..].Trim().ToString();

        var annotations = ParseAnnotations(source);
        return new TaxlFragment.Code(source, name, annotations);
    }

    private static TaxlFragment.Output ParseOutputFragment(SourceFileView source)
    {
        var text = source.TextSpan;
        Debug.Assert(text.StartsWith(OutputFragmentSeparator));
        
        var name = FirstLineText(text)[OutputFragmentSeparator.Length..].Trim().ToString();
        return new TaxlFragment.Output(source, name);
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
                var start = i;
                var end = NextNewlineOrEnd(text, start);

                var directiveSpan = source.SpanFromTo(start, end);
                directives.Add(ParseDirective(text[start..end], directiveSpan));

                i = end;
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

    private static TaxlDirective ParseDirective(ReadOnlySpan<char> text, SourceSpan span)
    {
        Debug.Assert(text.StartsWith(DirectiveStart));

        var name = text[DirectiveStart.Length..].Trim();

        var kind = name switch
        {
            "run-pass" => TaxlDirectiveKind.RunPass,
            "run-panic" => TaxlDirectiveKind.RunPanic,
            "check" => TaxlDirectiveKind.Check,
            _ => TaxlDirectiveKind.Unknown
        };

        return new TaxlDirective(span, kind);
    }


    private static ImmutableArray<TaxlAnnotation> ParseAnnotations(SourceFileView source)
        =>
        [
            .. Lexer.Lex(source, new DiagnosticBag())
                .Where(t => t.Kind is TokenKind.Comment)
                .Where(t => source.GetText(t.FullSpan).StartsWith(AnnotationStart))
                .Select(t => ParseAnnotation(source.GetText(t.FullSpan), source.GetLocation(t.FullSpan)))
        ];
    

    private static TaxlAnnotation ParseAnnotation(ReadOnlySpan<char> text, SourceLocation textLocation)
    {
        Debug.Assert(text.StartsWith(AnnotationStart));

        var index = AnnotationStart.Length;
        
        // Skip starting whitespace
        for (; index < text.Length; index++)
        {
            if (!char.IsWhiteSpace(text[index]))
                break;
        }

        // Diagnostics
        if (text[index..].StartsWith(ErrorAnnotation))
        {
            index += ErrorAnnotation.Length;
            return new TaxlAnnotation.Diagnostic(DiagnosticKind.Error, 
                Id: index < text.Length ? text[index..].Trim().ToString() : "", 
                textLocation.StartLinePosition.Line,
                textLocation.Span);
        }
        if (text[index..].StartsWith(LintAnnotation))
        {
            index += ErrorAnnotation.Length;
            return new TaxlAnnotation.Diagnostic(DiagnosticKind.Lint, 
                Id: index < text.Length ? text[index..].Trim().ToString() : "", 
                textLocation.StartLinePosition.Line,
                textLocation.Span);
        }

        if (text[index..].StartsWith(TypeAnnotation))
        {
            index += TypeAnnotation.Length;

            return ParseTypeAnnotation(text, afterAnnotationNameIndex: index, textLocation);
        }

        return new TaxlAnnotation.Invalid("Unknown annotation.", textLocation.Span);
    }

    private static TaxlAnnotation ParseTypeAnnotation(ReadOnlySpan<char> text, int afterAnnotationNameIndex, SourceLocation textLocation)
    {
        Debug.Assert(text.Length == textLocation.Span.Length, $"{nameof(text)} must represent {nameof(textLocation)}");
        
        // Carets relative to text
        var caretStartInText = text[afterAnnotationNameIndex..].IndexOf('^') + afterAnnotationNameIndex;
        if (caretStartInText < afterAnnotationNameIndex)
            return new TaxlAnnotation.Invalid("Type annotation must use '^' to point at an expression.", textLocation.Span);
        var caretLength = 1;
        
        for (; caretStartInText + caretLength < text.Length; caretLength++)
        {
            if (text[caretStartInText + caretLength] is not '^')
                break;
        }                         
        
        // Make relative to file
        var caretSpan = SourceSpan.InsideSourceFile(
            first: textLocation.Span.First + caretStartInText,
            caretLength);
        
        // Make relative to line
        var caretLine = textLocation.File.GetLineAt(caretSpan.First);
        Debug.Assert(caretLength <= caretLine.Span.Length, "One annotation is one line.");

        var inCaretLineStart = caretSpan.First - caretLine.Span.First;
        Debug.Assert(inCaretLineStart >= 0);
        
        // Retrieve line above
        if (caretLine.LineNumber <= 0)
        {
            return new TaxlAnnotation.Invalid(
                "Type annotation with carets on first line. It cannot point to line above.", textLocation.Span);
        }

        // Make relative to line above
        var lineAbove = textLocation.File.Lines[caretLine.LineNumber - 1];
        var referencedSpan = SourceSpan.InsideSourceFile(
            first: lineAbove.Span.First + inCaretLineStart,
            caretLength);
        if (!lineAbove.Span.Contains(referencedSpan))
        {
            return new TaxlAnnotation.Invalid(
                "Type annotation does not reference a valid position in line above.", textLocation.Span);
        }
        
        // Get type name
        var typeNameIndex = caretStartInText + caretLength;
        var typeName = typeNameIndex < text.Length
            ? text[typeNameIndex..].Trim().ToString()
            : "";

        return new TaxlAnnotation.Type(referencedSpan, typeName, textLocation.Span);
    }
    


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
            if (!lineHasContext && (rest[i..].StartsWith(CodeFragmentSeparator) || rest[i..].StartsWith(OutputFragmentSeparator)))
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