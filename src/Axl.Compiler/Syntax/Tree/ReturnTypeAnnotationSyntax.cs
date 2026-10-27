using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ReturnTypeAnnotationSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ReturnTypeAnnotation, children)
{
    public Token? MutKw => Children.OfType<Token>().FirstOrDefault(t => t.Kind is TokenKind.MutKw);
    
    public ExprSyntax TypeExpr => Children.FirstOfType<ExprSyntax>();
}