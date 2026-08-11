using System.Diagnostics;

namespace Axl.Compiler.Syntax;

/// <summary>
/// A coarse grouping of syntax, used where a specific <see cref="SyntaxKind"/> is
/// too precise. For example to say "an expression was expected here".
/// </summary>
public enum SyntaxCategory
{
    Expr,
    Stmt,
    Body,
    TypeName,
    Member,
    ParamList
}

public static class SyntaxCategoryExtensions
{
    extension(SyntaxCategory category)
    {
        /// <summary>
        /// How this category is named in diagnostic messages.
        /// </summary>
        public string DisplayName => category switch
        {
            SyntaxCategory.Expr => "an expression",
            SyntaxCategory.Stmt => "a statement",
            SyntaxCategory.Body => "a body",
            SyntaxCategory.TypeName => "a type name",
            SyntaxCategory.Member => "a member ('fn' or 'module')",
            SyntaxCategory.ParamList => "parameters",
            _ => throw new UnreachableException()
        };
    }
}