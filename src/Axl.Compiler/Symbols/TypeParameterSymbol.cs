using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public class TypeParameterSymbol(string name, Symbol owner, int ordinal, TypeParamSyntax? declarationSyntax) 
    : TypeSymbol(owner.Compilation, name, owner, isPublic: true)
    // Should be public, because it must be as visible as the function itself.
{
    public int Ordinal { get; } = ordinal;
    public TypeParamSyntax? DeclarationSyntax { get; } = declarationSyntax;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    // Type parameters have no members (yet). In the future, lookup on
    // implemented traits will happen here.
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];
}