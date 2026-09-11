using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public abstract record StringPart
{
    public sealed record Text(string ProcessedText) : StringPart;

    public sealed record Interpolation(BoundExpr Expr) : StringPart;
}

public sealed class BoundStringExpr(ImmutableArray<StringPart> parts, TypeSymbol type, SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public ImmutableArray<StringPart> Parts { get; } = parts;

    protected override ImmutableArray<BoundStmt> GetChildren() =>
    [
        .. Parts
            .OfType<StringPart.Interpolation>()
            .Select(interpolation => interpolation.Expr)
    ];

}