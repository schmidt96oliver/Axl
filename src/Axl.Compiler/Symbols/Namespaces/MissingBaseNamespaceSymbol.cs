using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public sealed class MissingBaseNamespaceSymbol(Symbol owner) 
    : NamespaceSymbol(owner.Compilation, name: "base", owner: owner, isRoot: false)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [];

    /// <summary>
    /// Since base namespace is missing, always return an error symbol
    /// to suppress further errors. A single "base namespace not found"
    /// is more useful.
    /// </summary>
    public override ImmutableArray<Symbol> LookupMembers(string name)
        => [Compilation.BaseLibrary.Error];
}