using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public class TypeParameterSymbol(string name, Symbol owner, int ordinal, TypeParamSyntax? declarationSyntax) 
    : TypeSymbol(owner.Compilation, name, owner, isPublic: true)
    // Should be public, because it must be as visible as the function/type itself.
{
    public TypeParamSyntax? DeclarationSyntax { get; } = declarationSyntax;

    public int Ordinal { get; } = ordinal;
    
    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    /// <inheritdoc />
    public override bool IsConcrete => false;

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments => [];
    
    // Type parameters have no members (yet). In the future, lookup on
    // implemented traits will happen here.
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override TypeSymbol Substitute(TypeMap map)
        => map.Map(this);
}