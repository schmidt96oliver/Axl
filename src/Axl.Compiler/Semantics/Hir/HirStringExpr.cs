using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public abstract record StringPart
{
    public sealed record Text(string ProcessedText) : StringPart;

    public sealed record Interpolation(HirExpr Expr) : StringPart;
}

public sealed class HirStringExpr(ImmutableArray<StringPart> parts, AxlType type) : HirExpr(type)
{
    public ImmutableArray<StringPart> Parts { get; } = parts;
}