using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class IfExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.IfExpr, children)
{
    public ExprSyntax Predicate => Children.FirstOfType<ExprSyntax>();

    // Body is an expression, but bodies are strictly not allowed
    // in predicate position and the parser never puts them there.
    public BodySyntax Body => Children.FirstOfType<BodySyntax>();

    public ExprSyntax? ElseBody => Children.FirstOfKindOrNull(SyntaxKind.ElseClause)?
        .Children.FirstOfType<ExprSyntax>();
}