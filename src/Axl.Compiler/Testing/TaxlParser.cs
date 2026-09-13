using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed class TaxlParser(SourceText sourceText, DiagnosticBag diagnostics)
{
    public Directive? ParseDirective()
    {
        var trimmedLineLocations = sourceText.Lines.Select(line => sourceText.GetLocation(TrimSpan(line.Range)));
        
        foreach (var trimmedLineLocation in trimmedLineLocations)
        {
            var text = trimmedLineLocation.Text;
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
        diagnostics.ReportError(new Diagnostic.MissingTaxlDirective(sourceText.GetLocation(sourceText.Lines[0].Range)));
        return null;
    }

    public ImmutableArray<Annotation> ParseAnnotations()
    {
        // Just run the lexer, since it already knows best where to find
        // the correct comments in sourceText text.

        var annotations = ImmutableArray.CreateBuilder<Annotation>();
        var annotationLocations = Lexer.Lex(sourceText, new DiagnosticBag())
            .Where(t => t.Kind is TokenKind.Comment)
            .Select(t => sourceText.GetLocation(t.FullRange))
            .Where(l => l.Text.StartsWith("//~"));

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
        var text = location.Text;

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

        var prefixRange = SourceRange.FromLength(location.Range.Start, prefixLength);
        return new DiagnosticAnnotation(
            FullLocation: location,
            PrefixLocation: new SourceLocation(location.SourceText, prefixRange),
            Kind: kind,
            id);
    }

    private TypeAnnotation? ParseTypeAnnotation(SourceLocation location)
    {
        var text = location.Text;
        if (!text.StartsWith("//~type"))
            return null;
        
        // --- Caret range
        var caretStart = text.IndexOf('^');
        if (caretStart < 0)
            return null;
        var caretLast = text.LastIndexOf('^');
        var caretRange = SourceRange.FromLength(location.Range.Start + caretStart,
            length: caretLast - caretStart + 1);
        
        // Allow only contiguous caret blocks
        if (text[caretStart..(caretLast + 1)].ContainsAnyExcept('^'))
            return null;
        
        // --- Project caret to line above
        var lineAbove = location.StartLine - 1;
        if (lineAbove < 0)
            return null;
        var referencedRange = SourceRange.FromLength(
            start: sourceText.Lines[lineAbove].First + caretRange.Start - sourceText.Lines[location.StartLine].First,
            length: caretRange.Length);
        if (!sourceText.Lines[lineAbove].Range.Contains(referencedRange))
            return null;
        
        var referencedLocation = sourceText.GetLocation(referencedRange);
        
        // --- Argument
        var typeName = text[(caretLast + 1)..].Trim().ToString();
        if (string.IsNullOrEmpty(typeName))
            return null;

        var prefixRange = SourceRange.FromLength(location.Range.Start, 
            length: caretLast + 1);
        return new TypeAnnotation(
            FullLocation: location,
            PrefixLocation: sourceText.GetLocation(prefixRange),
            referencedLocation,
            typeName);
    }
    
    
    private SourceRange TrimSpan(SourceRange range)
    {
        var text = sourceText.GetText(range);
        
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
        return SourceRange.FromLength(range.Start + start, end - start + 1);
    }
    
    
    #region Old Fragment logic
    
    // Kept here for future reference, when i'm building the file splitter again.
    
    // public ImmutableArray<Fragment> ParseFragments()
    // {
    //     if (sourceText.Lines.Length == 0)
    //         return [new Fragment(SourceFileView.Whole(sourceText), "", false, [])];
    //     
    //     var delimiterIndices = sourceText.Lines
    //         .Where(line => sourceText.GetText(line.Range).TrimStart() is
    //             ['/', '/', '-', '-', '-', ..] or ['/', '/', '=', '=', '=', ..])
    //         .Select(line => line.LineNumber)
    //         .ToList();
    //     
    //     if (delimiterIndices.Count == 0)
    //         return [ParseFragment(0, sourceText.Lines.Length - 1)];
    //
    //     var fragments = ImmutableArray.CreateBuilder<Fragment>();
    //     
    //     // First fragment has no delimiter
    //     if (delimiterIndices[0] > 0)
    //     {
    //         var fragment = ParseFragment(0, delimiterIndices[0] - 1);
    //         fragments.Add(fragment);
    //     }
    //     
    //     for (var i = 0; i < delimiterIndices.Count; i++)
    //     {
    //         var firstLine = delimiterIndices[i];
    //         var lastLine = i + 1 < delimiterIndices.Count
    //             ? delimiterIndices[i + 1] - 1
    //             : sourceText.Lines.Length - 1;
    //         
    //         var fragment = ParseFragment(firstLine, lastLine);
    //         fragments.Add(fragment);
    //     }
    //
    //     return fragments.DrainToImmutable();
    // }
    // private Fragment ParseFragment(int firstLine, int lastLine)
    // {
    //     Debug.Assert(firstLine <= lastLine && firstLine >= 0 && lastLine < sourceText.Lines.Length);
    //
    //     // Get sourceText view
    //     var firstIndex = sourceText.Lines[firstLine].Range.First;
    //     var endIndex = sourceText.Lines[lastLine].Range.End;
    //
    //     var range = SourceRange.FromBounds(firstIndex, endIndex);
    //     var sourceView = new SourceFileView(sourceText, range);
    //     
    //     // Read headline
    //     var headlineText = sourceText.GetText(sourceText.Lines[firstLine].Range).TrimStart();
    //     var name = headlineText.StartsWith("//===") || headlineText.StartsWith("//---")
    //         ? headlineText[5..].Trim().ToString()
    //         : "";
    //     
    //     var isOutput = headlineText.StartsWith("//===");
    //     if (isOutput)   
    //         return new Fragment(sourceView, name, isOutput, []);
    //     
    //     // Parse annotations
    //     var annotations = ParseAnnotations(sourceView);
    //     return new Fragment(sourceView, name, IsOutput: false, annotations);
    // }
    //

    #endregion
}