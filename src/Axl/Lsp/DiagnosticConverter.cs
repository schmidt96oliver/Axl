using System.Collections.Immutable;
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
    public static Container<LspDiagnostic> Convert(ImmutableArray<AxlDiagnostic> diagnostics)
        => new(diagnostics.SelectMany(Convert));

    // Every underline carries the full message.
    private static IEnumerable<LspDiagnostic> Convert(AxlDiagnostic diagnostic)
        => diagnostic.Locations.Select((location, i) => new LspDiagnostic
        {
            Severity = diagnostic.DefaultSeverity switch
            {
                AxlSeverity.Error => DiagnosticSeverity.Error,
                AxlSeverity.Warning => DiagnosticSeverity.Warning,
                _ => DiagnosticSeverity.Information
            },

            // Add the hint to message. Lsp has no specification for hints.
            Message = diagnostic.Hint is null
                ? diagnostic.Message
                : $"{diagnostic.Message}\nHint: {diagnostic.Hint}",

            Code = new DiagnosticCode(diagnostic.Id),

            Range = location.ToLsp(),

            RelatedInformation = RelatedInformation(diagnostic, exceptLocation: i),
        });

    /// <summary>
    /// The other locations at fault, followed by the merely explaining ones.
    /// The siblings have no label of their own, so they borrow the message -
    /// that's what they read as when one underline links to the next.
    /// </summary>
    private static Container<DiagnosticRelatedInformation> RelatedInformation(
        AxlDiagnostic diagnostic, int exceptLocation)
        => new(diagnostic.Locations
            .Where((_, i) => i != exceptLocation)
            .Select(location => new Compiler.Diagnostics.LabeledSourceLocation(location, diagnostic.Message))
            .Concat(diagnostic.Related)
            // Only publish related infos, if the SourceFile has a file system
            // path. Otherwise, we don't know where to point.
            .Where(label => label.Location.File.Path is not null)
            .Select(label => new DiagnosticRelatedInformation
            {
                Location = new Location
                {
                    Range = label.Location.ToLsp(),
                    Uri = DocumentUri.FromFileSystemPath(label.Location.File.Path!)
                },
                Message = diagnostic.LocationLabel
            }));
}