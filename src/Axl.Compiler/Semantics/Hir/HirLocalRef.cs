using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

/// <summary>
/// A reference to a <see cref="LocalSymbol"/>. Note that failed lookups
/// are represented as <see cref="HirErrorExpr"/>.
/// </summary>
public sealed class HirLocalRef(LocalSymbol localSymbol, SyntaxNode syntax) : HirExpr(localSymbol.Type, syntax)
{
    public LocalSymbol LocalSymbol { get; } = localSymbol;
}