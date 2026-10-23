using System.Net;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Lsp;

using AxlDiagnostic = Compiler.Diagnostics.Diagnostic;

public class DiagnosticHandler : DocumentDiagnosticHandlerBase
{
    /// <inheritdoc />
    protected override DiagnosticsRegistrationOptions CreateRegistrationOptions(DiagnosticClientCapabilities capability,
        ClientCapabilities clientCapabilities)
    {
        return new DiagnosticsRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId),
            Identifier = "axl",
            InterFileDependencies = true,
            WorkspaceDiagnostics = false,
        };
    }

    /// <inheritdoc />
    public override Task<RelatedDocumentDiagnosticReport> Handle(DocumentDiagnosticParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;

        if (ProjectStore.TryGetProject(uri) is not IProject project)
        {
            // Project could not be found, return no errors.
            return Task.FromResult<RelatedDocumentDiagnosticReport>(new RelatedFullDocumentDiagnosticReport
            {
                Items = new Container<Diagnostic>()
            });
        }

        return Task.FromResult<RelatedDocumentDiagnosticReport>(new RelatedFullDocumentDiagnosticReport
        {
            Items = new Container<Diagnostic>(project.Compilation.Diagnostics
                .SelectMany(axlDiagnostic => ToLsp(axlDiagnostic, uri)))
        });
    }

    
    /// <summary>
    /// If possible, expands <paramref name="location"/> to one character, if it is empty.
    /// Does nothing, if it is not empty. This is necessary, to prevent the editor
    /// from snapping to the nearest word or similar.
    /// </summary>
    private static SourceLocation ImproveLocationForEditor(SourceLocation location)
    {
        if (location.Length > 0)
            return location;

        // The editor tends to auto-snap empty ranges to the nearest word
        // or weirder things. So if possible, we expand those ranges by one
        // character, so it points to whitespace, which looks much nicer.
        // We must not cross line boundaries, so as a backup, we can go
        // back one character. If nothing is possible, just leave it to
        // the editor.
        
        var endLine = location.SourceText.Lines[location.EndLine];

        if (location.EndColumn < endLine.Length)
            return SourceLocation.FromBounds(location.SourceText, location.Start, location.End + 1);
        if (location.StartColumn > 0)
            return SourceLocation.FromBounds(location.SourceText, location.Start - 1, location.End);
        return location;
    }


    public static IEnumerable<Diagnostic> ToLsp(AxlDiagnostic diagnostic, DocumentUri uri)
    {
        var message = diagnostic.Hint is null
            ? diagnostic.Message
            : $"{diagnostic.Message}\nHint: {diagnostic.Hint}"; 
        var messageLines = message.Split('\n');

        // This is a workaround to displaying multiple lines: We display
        // each line as one related info without link, because Rider with LSP4IJ
        // cannot display multiple lines. If I ever try VSCode, it might work better.
        
        var messageRelatedInfo = messageLines[1..].Select(line => new DiagnosticRelatedInformation
        {
            Location = null!,
            Message = WebUtility.HtmlEncode(line)
        }).ToList();

        if (diagnostic.Locations.Length == 0)
            return [TrySingleLocationToLsp(diagnostic, uri, -1, messageLines[0], messageRelatedInfo)!];
        
        return Enumerable.Range(0, diagnostic.Locations.Length)
            .Select(i => TrySingleLocationToLsp(diagnostic, uri, i, messageLines[0], messageRelatedInfo))
            .Where(lspDiagnostic => lspDiagnostic is not null)!;
    }

    private static Diagnostic? TrySingleLocationToLsp(AxlDiagnostic diagnostic, DocumentUri uri, int locationIndex, string message, IReadOnlyList<DiagnosticRelatedInformation> previousRelatedInformation)
    {
        Location? lspLocation;
        if (locationIndex < 0)
        {
            // The diagnostic has no location, so add it on
            // top of every file.
            lspLocation = new Location { Uri = uri, Range = new Range(0, 0, 0, 10) };
        }
        else
        {
            lspLocation = ProjectStore.TryMapToLsp(ImproveLocationForEditor(diagnostic.Locations[locationIndex]));
            if (lspLocation is null || lspLocation.Uri != uri)
                return null;
        }

        return new Diagnostic
        {
            Severity = diagnostic.DefaultSeverity switch
            {
                Compiler.Diagnostics.DiagnosticSeverity.Error => DiagnosticSeverity.Error,
                Compiler.Diagnostics.DiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                _ => DiagnosticSeverity.Information
            },

            Tags = GetTags(diagnostic),
            
            // Only the first line, as stated above.
            Message = message,

            Code = new DiagnosticCode(diagnostic.Id),
            Range = lspLocation!.Range,
            RelatedInformation = new Container<DiagnosticRelatedInformation>(
            [
                .. previousRelatedInformation, .. GetRelatedInformation(diagnostic, exceptIndex: locationIndex)
            ]),
        };
    }

    private static Container<DiagnosticTag> GetTags(AxlDiagnostic diagnostic)
        => diagnostic is AxlDiagnostic.UnnecessaryUsing
            ? new Container<DiagnosticTag>(DiagnosticTag.Unnecessary)
            : new Container<DiagnosticTag>();
    
    private static IEnumerable<DiagnosticRelatedInformation> GetRelatedInformation(AxlDiagnostic diagnostic, int exceptIndex)
        => diagnostic.Locations
            // Other primary locations become related.
            .Where((_, i) => i != exceptIndex)
            .Select(axlLocation => new Compiler.Diagnostics.LabeledSourceLocation(axlLocation, diagnostic.LocationLabel))
            .Concat(diagnostic.Related)
            
            .Select(label => (LspLocation: ProjectStore.TryMapToLsp(ImproveLocationForEditor(label.Location)), Message: label.Label))
            .Where(lspLocationAndMessage => lspLocationAndMessage.LspLocation is not null)
            .Select(lspLocationAndMessage => new DiagnosticRelatedInformation
            {
                Location = lspLocationAndMessage.LspLocation!,
                Message = lspLocationAndMessage.Message
            });
}