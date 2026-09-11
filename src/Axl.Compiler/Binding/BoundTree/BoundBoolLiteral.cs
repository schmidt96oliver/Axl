using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundBoolLiteral(bool value, TypeSymbol type, SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public bool Value { get; } = value;

    protected override ImmutableArray<BoundStmt> GetChildren() => [];

}