using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public closed class FunDefinitionSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic)
    : FunSymbol(compilation, name, owner, isPublic)
{
    // This symbol IS the original definition.
    /// <inheritdoc />
    public override FunDefinitionSymbol OriginalDefinition => this;
    
    // This symbol IS the original definition.
    /// <inheritdoc />
    public override FunSymbol? ConstructedFrom => null;

    // The definition does not have type arguments yet.
    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments => [];

    public abstract FunDeclSyntax? DeclarationSyntax { get; }

    public abstract FunBody Body { get; }


    // Since this _IS_ the original definition, and it is source declared,
    // we can override and compare by reference for performance. It keeps
    // the same behaviour as the override on FunSymbol.
    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => ReferenceEquals(this, other);

    /// <inheritdoc />
    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(this);
}