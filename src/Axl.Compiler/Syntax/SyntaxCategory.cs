namespace Axl.Compiler.Syntax;

/// <summary>
/// A coarse grouping of syntax, used where a specific <see cref="SyntaxKind"/> is
/// too precise. For example to say "an expression was expected here".
/// </summary>
public enum SyntaxCategory
{
    Expr,
    Stmt,
}
