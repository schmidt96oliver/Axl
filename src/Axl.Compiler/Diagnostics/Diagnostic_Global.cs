using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

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

    public sealed record CannotFindBaseType(BaseTypeKind Kind, ImmutableArray<Symbol> Candidates) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [];

        /// <inheritdoc />
        public override string Message => Candidates.Length == 0
            ? $"Cannot find type '{Names.BaseNamespace}.{Kind}'."
            : $"Base type '{Names.BaseNamespace}.{Kind}' is ambiguous between {Candidates.Length} candidates.";
    }
}