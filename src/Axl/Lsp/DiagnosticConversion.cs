using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public static class DiagnosticConversion
{
    extension(Compiler.Diagnostics.Diagnostic diagnostic)
    {
        public Diagnostic ToLsp() => new()
        {
            Severity = diagnostic.DefaultSeverity switch
            {
                Compiler.Diagnostics.DiagnosticSeverity.Error
                    => DiagnosticSeverity.Error,
                Compiler.Diagnostics.DiagnosticSeverity.Warning
                    => DiagnosticSeverity.Warning,
                _ => DiagnosticSeverity.Information
            },

            // Add the hint to message. Lsp has no specification for hints.
            Message = diagnostic.Hint is null
                ? diagnostic.Message
                : $"{diagnostic.Message}\nHint: {diagnostic.Hint}",

            Code = new DiagnosticCode(diagnostic.Id),

            Range = diagnostic.Location.ToLsp(),

            // Only publish related infos, if the SourceFile has
            // a file system path. Otherwise, we don't know where to
            // point.
            RelatedInformation = new Container<DiagnosticRelatedInformation>(
                diagnostic.Related
                    .Where(related => related.Location.File.Path is not null)
                    .Select(related => new DiagnosticRelatedInformation
                        {
                            Location = new Location
                            {
                                Range = related.Location.ToLsp(),
                                Uri = DocumentUri.FromFileSystemPath(related.Location.File.Path!)
                            },
                            Message = related.Label
                        })
            ),
        };

    }

    extension(ImmutableArray<Compiler.Diagnostics.Diagnostic> diagnostics)
    {
        public Container<Diagnostic> ToLsp()
            => new Container<Diagnostic>(diagnostics.Select(d => d.ToLsp()));
    }
}