using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Hir;

/// <summary>
/// A reference to a <see cref="LocalSymbol"/>. Note that failed lookups
/// are represented as <see cref="HirErrorExpr"/>.
/// </summary>
public sealed class HirLocalRef(LocalSymbol localSymbol) : HirExpr(localSymbol.Type)
{
    public LocalSymbol LocalSymbol { get; } = localSymbol;
}