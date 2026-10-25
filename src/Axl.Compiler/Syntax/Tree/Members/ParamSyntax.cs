using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ParamSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Param, children)
{
    public Token? MutKw => Children.OfType<Token>().FirstOrDefault(t => t .Kind is TokenKind.MutKw);
    
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;

    public ExprSyntax TypeAnnotation => Children
        .FirstOfType<TypeAnnotationClauseSyntax>().TypeExpr;
}