using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirNumberLiteral(NumberLiteralToken token, TypeSymbol type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public NumberLiteralToken Token { get; } = token;

    protected override ImmutableArray<HirStmt> GetChildren() => [];

}