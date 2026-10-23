using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class MissingBaseNamespaceSymbol(Symbol owner) 
    : NamespaceSymbol(name: "base", owner, isRoot: false)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];

    /// <summary>
    /// Since base namespace is missing, always return an error symbol
    /// to suppress further errors. A single "base namespace not found"
    /// is more useful.
    /// </summary>
    public override ImmutableArray<Symbol> LookupMembers(string name)
        => [ErrorTypeSymbol.Instance];
}