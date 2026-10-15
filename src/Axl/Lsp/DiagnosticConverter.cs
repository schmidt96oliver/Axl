using System.Collections.Immutable;
using System.Net;
using Axl.Compiler;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using AxlDiagnostic = Axl.Compiler.Diagnostics.Diagnostic;
using AxlSeverity = Axl.Compiler.Diagnostics.DiagnosticSeverity;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;

namespace Axl.Lsp;

/// <summary>
/// Converts compiler diagnostics into the ones we publish over LSP.
/// </summary>
/// <remarks>
/// This is not a one-to-one mapping: LSP has no concept of a single diagnostic
/// owning several ranges, so a compiler diagnostic with several
/// <see cref="AxlDiagnostic.Locations"/> becomes one LSP diagnostic per
/// location. Each of them points at its siblings.
/// </remarks>
public static class DiagnosticConverter
{
    public static IEnumerable<LspDiagnostic> Convert(IEnumerable<AxlDiagnostic> diagnostics, DocumentUri uri)
        => diagnostics.SelectMany(d => Convert(d, uri));


    private static IEnumerable<LspDiagnostic> Convert(AxlDiagnostic diagnostic, DocumentUri uri)
    {
        var msg = diagnostic.Hint is null
            ? diagnostic.Message
            : $"{diagnostic.Message}\nHint: {diagnostic.Hint}"; 
        var msgLines = msg.Split('\n');

        // This is a workaround to displaying multiple lines: We display
        // each line as one related info without link, because Rider with LSP4IJ
        // cannot display multiple lines. If I ever try VSCode, it might work better.
        
        var msgCtd = msgLines[1..].Select(line => new DiagnosticRelatedInformation
        {
            Location = null!,
            Message = WebUtility.HtmlEncode(line)
        });

        return diagnostic.Locations.Select(ProjectStore.TryMapToLsp)
            .Where(lspLocation => lspLocation is not null && lspLocation.Uri == uri)
            .Select((location, i) => new LspDiagnostic
            {
                Severity = diagnostic.DefaultSeverity switch
                {
                    AxlSeverity.Error => DiagnosticSeverity.Error,
                    AxlSeverity.Warning => DiagnosticSeverity.Warning,
                    _ => DiagnosticSeverity.Information
                },

                // Add the hint to message. Lsp has no specification for hints.
                Message = msgLines[0],

                Code = new DiagnosticCode(diagnostic.Id),

                Range = location!.Range,

                RelatedInformation = new([.. msgCtd, .. RelatedInformation(diagnostic, exceptLocation: i)]),
            });
    }
    
    private static IEnumerable<DiagnosticRelatedInformation> RelatedInformation(
        AxlDiagnostic diagnostic, int exceptLocation)
        => diagnostic.Locations
            .Where((_, i) => i != exceptLocation)
            .Select(axlLocation => new Compiler.Diagnostics.LabeledSourceLocation(axlLocation, diagnostic.LocationLabel))
            .Concat(diagnostic.Related)
            .Select(label => (LspLocation: ProjectStore.TryMapToLsp(label.Location), Message: label.Label))
            .Where(lspLocationAndMessage => lspLocationAndMessage.LspLocation is not null)
            .Select(lspLocationAndMessage => new DiagnosticRelatedInformation
            {
                Location = lspLocationAndMessage.LspLocation!,
                Message = lspLocationAndMessage.Message
            });
}