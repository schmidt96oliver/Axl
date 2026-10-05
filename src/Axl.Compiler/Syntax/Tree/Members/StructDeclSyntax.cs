using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class StructDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.StructDecl, children)
{
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;
    
    public StructBodySyntax? Body => Children.FirstOfTypeOrNull<StructBodySyntax>();
}