using System.Collections.Immutable;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class ParameterDefinition(
    string name,
    FunSymbol owner,
    TypeSymbol type,
    bool isMut,
    bool isSelf,
    ParamSyntax? declarationSyntax = null)
    : Definition<ParameterSymbol>(owner.Compilation, name, owner, isPublic: false)
{
    public TypeSymbol Type { get; } = type;

    public bool IsMut { get; } = isMut;

    public bool IsSelf { get; } = isSelf;

    public ParamSyntax? DeclarationSyntax { get; } = declarationSyntax;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax =>
        DeclarationSyntax is not null ? [DeclarationSyntax] : [];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax =>
        DeclarationSyntax is not null ? [DeclarationSyntax.Name] : [];

    protected override ParameterSymbol Create(Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments)
        => new(this, (FunSymbol)owner!, isIdentity: typeArguments is null);
}