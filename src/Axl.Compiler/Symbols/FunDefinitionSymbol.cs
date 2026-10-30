using System.Collections.Immutable;
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
}