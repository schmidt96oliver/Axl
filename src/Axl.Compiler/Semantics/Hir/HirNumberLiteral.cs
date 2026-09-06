using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirNumberLiteral(NumberLiteralToken token, AxlType type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public NumberLiteralToken Token { get; } = token;
}