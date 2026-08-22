using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class UsingDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.UsingDecl, children)
{
    public PathSyntax Name => Children.FirstOfType<PathSyntax>();
}