using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class VarDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.VarDecl, children)
{
    public IdentifierToken Name => NthChildOfType<IdNameSyntax>(0).Token;

    public TypeNameSyntax? TypeAnnotation => NthNodeOfKindOrNull(SyntaxKind.TypeAnnotationClause, 0)?
        .NthChildOfType<TypeNameSyntax>(0);
    
    public ExprSyntax? Initializer => NthNodeOfKindOrNull(SyntaxKind.InitializerClause, 0)?
        .NthChildOfType<ExprSyntax>(0);
}