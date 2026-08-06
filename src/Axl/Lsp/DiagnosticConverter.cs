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
/// owning several ranges, so a compiler diagnostic with primary related
/// locations (see <see cref="Compiler.Diagnostics.LabeledSourceLocation.IsPrimary"/>)
/// becomes one LSP diagnostic per primary location. That's why the conversion
/// only works on the whole list - a single diagnostic can't answer with a
/// single diagnostic.
/// </remarks>
public static class DiagnosticConverter
{
    public static Container<LspDiagnostic> Convert(ImmutableArray<AxlDiagnostic> diagnostics)
        => new(diagnostics.SelectMany(Convert));

    private static IEnumerable<LspDiagnostic> Convert(AxlDiagnostic diagnostic)
    {
        var labels = Labels(diagnostic);

        for (var i = 0; i < labels.Count; i++)
        {
            if (!labels[i].IsPrimary)
                continue;

            // Every underline carries the full message. The label alone
            // ("Conflicts with this operator.") is meaningless in the flat
            // list of a problems panel.
            yield return new LspDiagnostic
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

                Range = labels[i].Location.ToLsp(),

                RelatedInformation = RelatedInformation(labels, except: i),
            };
        }
    }

    /// <summary>
    /// All labeled locations of <paramref name="diagnostic"/>, its own location
    /// first. The main location has no label of its own, so it borrows the
    /// message - that's what it reads as when another underline links to it.
    /// </summary>
    private static IReadOnlyList<Compiler.Diagnostics.LabeledSourceLocation> Labels(AxlDiagnostic diagnostic)
        =>
        [
            new(diagnostic.Location, diagnostic.Message, IsPrimary: true),
            .. diagnostic.Related
        ];

    private static Container<DiagnosticRelatedInformation> RelatedInformation(
        IReadOnlyList<Compiler.Diagnostics.LabeledSourceLocation> labels, int except)
        => new(labels
            .Where((_, i) => i != except)
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
                Message = label.Label
            }));
}
