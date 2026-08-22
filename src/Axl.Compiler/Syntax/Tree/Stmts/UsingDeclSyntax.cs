using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class UsingDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.UsingDecl, children)
{
    public QualifiedNameSyntax Name => NthChildOfType<QualifiedNameSyntax>(0);
}