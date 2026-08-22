using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class VarDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.VarDecl, children)
{
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;

    public TypeNameSyntax? TypeAnnotation => Children
        .FirstOfKindOrNull(SyntaxKind.TypeAnnotationClause)?
        .Children.FirstOfType<TypeNameSyntax>();
    
    public ExprSyntax? Initializer => Children
        .FirstOfKindOrNull(SyntaxKind.InitializerClause)?
        .Children.FirstOfType<ExprSyntax>();
}