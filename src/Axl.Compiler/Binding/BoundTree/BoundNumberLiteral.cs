using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundNumberLiteral(NumberLiteralToken token, TypeSymbol type, SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public NumberLiteralToken Token { get; } = token;

    protected override ImmutableArray<BoundStmt> GetChildren() => [];

}