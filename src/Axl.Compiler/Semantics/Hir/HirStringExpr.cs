using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public abstract record StringPart
{
    public sealed record Text(string ProcessedText) : StringPart;

    public sealed record Interpolation(HirExpr Expr) : StringPart;
}

public sealed class HirStringExpr(ImmutableArray<StringPart> parts, AxlType type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public ImmutableArray<StringPart> Parts { get; } = parts;
}