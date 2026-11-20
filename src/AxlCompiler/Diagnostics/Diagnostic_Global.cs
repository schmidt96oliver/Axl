using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Text;

namespace AxlCompiler.Diagnostics;

public closed partial record Diagnostic
{
    public sealed record CannotFindBaseNamespace(ImmutableArray<NamespaceSymbol> Candidates) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [];

        /// <inheritdoc />
        public override string Message => Candidates.Length == 0
            ? $"Cannot find '{Names.BaseNamespace}' namespace."
            : $"'{Names.BaseNamespace}' namespace is ambiguous between {Candidates.Length} candidates.";
    }

    public sealed record CannotFindBaseType(KnownName KnownName, ImmutableArray<Symbol> Candidates) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [];

        /// <inheritdoc />
        public override string Message => Candidates.Length == 0
            ? $"Cannot find type '{Names.BaseNamespace}.{KnownName}'."
            : $"Base type '{Names.BaseNamespace}.{KnownName}' is ambiguous between {Candidates.Length} candidates.";
    }
}