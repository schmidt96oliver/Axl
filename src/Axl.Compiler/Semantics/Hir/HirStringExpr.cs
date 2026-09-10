using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public abstract record StringPart
{
    public sealed record Text(string ProcessedText) : StringPart;

    public sealed record Interpolation(HirExpr Expr) : StringPart;
}

public sealed class HirStringExpr(ImmutableArray<StringPart> parts, TypeSymbol type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public ImmutableArray<StringPart> Parts { get; } = parts;

    protected override ImmutableArray<HirStmt> GetChildren() =>
    [
        .. Parts
            .OfType<StringPart.Interpolation>()
            .Select(interpolation => interpolation.Expr)
    ];

}