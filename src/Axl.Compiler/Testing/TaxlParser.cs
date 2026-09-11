using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed class TaxlParser(SourceFile source, DiagnosticBag diagnostics)
{
    private SourceSpan TrimSpan(SourceSpan span)
    {
        var text = source.GetText(span);
        
        var start = 0;
        for (; start < text.Length; start++)
        {
            if (!char.IsWhiteSpace(text[start]))
                break;
        }

        var end = text.Length - 1;
        for (; end > start; end--)
        {
            if (!char.IsWhiteSpace(text[end]))
                break;
        }
        return SourceSpan.InsideSourceFile(span.First + start, end - start + 1);
    }
    
    
    public ImmutableArray<Fragment> ParseFragments()
    {
        if (source.Lines.Length == 0)
            return [new Fragment(SourceFileView.Whole(source), "", false, [])];
        
        var delimiterIndices = source.Lines
            .Where(line => source.GetText(line.Span).TrimStart() is
                ['/', '/', '-', '-', '-', ..] or ['/', '/', '=', '=', '=', ..])
            .Select(line => line.LineNumber)
            .ToList();
        
        if (delimiterIndices.Count == 0)
            return [ParseFragment(0, source.Lines.Length - 1)];

        var fragments = ImmutableArray.CreateBuilder<Fragment>();
        
        // First fragment has no delimiter
        if (delimiterIndices[0] > 0)
        {
            var fragment = ParseFragment(0, delimiterIndices[0] - 1);
            fragments.Add(fragment);
        }
        
        for (var i = 0; i < delimiterIndices.Count; i++)
        {
            var firstLine = delimiterIndices[i];
            var lastLine = i + 1 < delimiterIndices.Count
                ? delimiterIndices[i + 1] - 1
                : source.Lines.Length - 1;
            
            var fragment = ParseFragment(firstLine, lastLine);
            fragments.Add(fragment);
        }

        return fragments.DrainToImmutable();
    }

    public Directive? ParseDirective()
    {
        var trimmedLineLocations = source.Lines.Select(line => new SourceLocation(source, TrimSpan(line.Span)));
        
        foreach (var trimmedLineLocation in trimmedLineLocations)
        {
            var text = trimmedLineLocation.GetText();
            if (text.StartsWith("//@"))
            {
                var directive = text switch
                {
                    "//@check" => new Directive(DirectiveKind.Check, trimmedLineLocation),
                    "//@run-pass" => new Directive(DirectiveKind.RunPass, trimmedLineLocation),
                    "//@run-panic" => new Directive(DirectiveKind.RunPanic, trimmedLineLocation),
                    _ => null
                };
                if (directive is null)
                    diagnostics.ReportError(new Diagnostic.UnknownTaxlDirective(trimmedLineLocation));
                return directive;
            }

            // Comments and empty lines are skipped. Everything else
            // will block directives.
            if (text is not ("" or ['/', '/', ..]))
                goto missing;
        }

        missing:
        diagnostics.ReportError(new Diagnostic.MissingTaxlDirective(new SourceLocation(source, source.Lines.Length == 0
            ? SourceSpan.InsideSourceFile(0, 0)
            : source.Lines[0].Span)));
        return null;
    }
    
    
    private Fragment ParseFragment(int firstLine, int lastLine)
    {
        Debug.Assert(firstLine <= lastLine && firstLine >= 0 && lastLine < source.Lines.Length);

        // Get source view
        var firstIndex = source.Lines[firstLine].Span.First;
        var endIndex = source.Lines[lastLine].Span.End;

        var span = SourceSpan.InsideSourceFile(firstIndex, length: endIndex - firstIndex);
        var sourceView = new SourceFileView(source, span);
        
        // Read headline
        var headlineText = source.GetText(source.Lines[firstLine].Span).TrimStart();
        var name = headlineText.StartsWith("//===") || headlineText.StartsWith("//---")
            ? headlineText[5..].Trim().ToString()
            : "";
        
        var isOutput = headlineText.StartsWith("//===");
        if (isOutput)   
            return new Fragment(sourceView, name, isOutput, []);
        
        // Parse annotations
        var annotations = ParseAnnotations(sourceView);
        return new Fragment(sourceView, name, IsOutput: false, annotations);
    }
    
    private ImmutableArray<Annotation> ParseAnnotations(SourceFileView fragmentView)
    {
        // Just run the lexer, since it already knows best where to find
        // the correct comments in source text.

        var annotations = ImmutableArray.CreateBuilder<Annotation>();
        var annotationLocations = Lexer.Lex(fragmentView, new DiagnosticBag())
            .Where(t => t.Kind is TokenKind.Comment)
            .Where(t => fragmentView.GetText(t.FullSpan).StartsWith("//~"))
            .Select(t => fragmentView.GetLocation(t.FullSpan));

        foreach (var location in annotationLocations)
        {
            var annotation = (Annotation?)ParseDiagnosticAnnotation(location)
                             ?? ParseTypeAnnotation(location);
            if (annotation is null)
                diagnostics.ReportError(new Diagnostic.InvalidTaxlAnnotation(location));
            else
                annotations.Add(annotation);
        }

        return annotations.DrainToImmutable();
    }
    
    private DiagnosticAnnotation? ParseDiagnosticAnnotation(SourceLocation location)
    {
        var text = location.GetText();

        DiagnosticKind kind;
        int prefixLength;

        if (text.StartsWith("//~error"))
        {
            kind = DiagnosticKind.Error;
            prefixLength = "//~error".Length;
        }
        else if (text.StartsWith("//~lint"))
        {
            kind = DiagnosticKind.Lint;
            prefixLength = "//~lint".Length;
        }
        else
        {
            return null;
        }

        var id = prefixLength < text.Length 
            ? text[prefixLength..].Trim().ToString()
            : "";
        if (string.IsNullOrEmpty(id))
            return null;

        var prefixSpan = SourceSpan.InsideSourceFile(location.Span.First, prefixLength);
        return new DiagnosticAnnotation(
            FullLocation: location,
            PrefixLocation: new SourceLocation(location.File, prefixSpan),
            Kind: kind,
            id);
    }

    private TypeAnnotation? ParseTypeAnnotation(SourceLocation location)
    {
        var text = location.GetText();
        if (!text.StartsWith("//~type"))
            return null;
        
        // --- Caret span
        var caretStart = text.IndexOf('^');
        if (caretStart < 0)
            return null;
        var caretLast = text.LastIndexOf('^');
        var caretSpan = SourceSpan.InsideSourceFile(location.Span.First + caretStart,
            length: caretLast - caretStart + 1);
        
        // Allow only contiguous caret blocks
        if (text[caretStart..(caretLast + 1)].ContainsAnyExcept('^'))
            return null;
        
        // --- Location reference
        var lineStart = source.GetLineAt(location.Span.First).Span.First;
        var lineAbove = location.StartLinePosition.Line - 1;
        if (lineAbove < 0)
            return null;
        var referencedSpan = SourceSpan.InsideSourceFile(
            first: source.Lines[lineAbove].Span.First + caretSpan.First - lineStart,
            length: caretSpan.Length);
        if (!source.Lines[lineAbove].Span.Contains(referencedSpan))
            return null;
        
        var referencedLocation = new SourceLocation(source, referencedSpan);
        
        // --- Argument
        var typeName = text[(caretLast + 1)..].Trim().ToString();
        if (string.IsNullOrEmpty(typeName))
            return null;

        var prefixSpan = SourceSpan.InsideSourceFile(location.Span.First, 
            length: caretLast + 1);
        return new TypeAnnotation(
            FullLocation: location,
            PrefixLocation: new SourceLocation(source, prefixSpan),
            referencedLocation,
            typeName);
    }
}