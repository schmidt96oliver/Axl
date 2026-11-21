using System.Collections.Immutable;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class IterationVariableSymbol(FunSymbol owner, TypeSymbol type, ForStmtSyntax syntax)
    : LocalSymbol(syntax.IterationVariableName.Text.ToString(), owner)
{
    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [syntax];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [syntax.IterationVariableName];

    /// <inheritdoc />
    public override PlaceKind PlaceKind => PlaceKind.ReadOnlyPlace;

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;
}