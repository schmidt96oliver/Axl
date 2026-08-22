using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

/// <summary>
/// Using is not really a stmt, but it can appear in every stmt
/// position, so it's convenient to have it derive from stmt.
/// </summary>
public sealed class UsingDeclSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.UsingDecl, children)
{
    public PathSyntax Name => Children.FirstOfType<PathSyntax>();
}