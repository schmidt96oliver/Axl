using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirBoolLiteral(bool value, TypeSymbol type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public bool Value { get; } = value;

    protected override ImmutableArray<HirStmt> GetChildren() => [];

}