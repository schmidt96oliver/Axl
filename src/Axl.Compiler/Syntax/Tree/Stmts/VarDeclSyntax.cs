using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax.Tree;

public sealed class VarDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.VarDecl, children)
{
    public Token VarOrLetKwToken
    {
        get
        {
            var firstElement = SyntaxElements().First();
            Debug.Assert(firstElement is Token { Kind: TokenKind.LetKw or TokenKind.VarKw });
            return (Token)firstElement;
        }
    }
    
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;

    public TypeNameSyntax? TypeAnnotation => Children
        .FirstOfTypeOrNull<TypeAnnotationClauseSyntax>()?
        .TypeName;

    public ExprSyntax? Initializer => Children
        .FirstOfTypeOrNull<InitializerClauseSyntax>()?.Expr;
}