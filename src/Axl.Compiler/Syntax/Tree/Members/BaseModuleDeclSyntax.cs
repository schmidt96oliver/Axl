using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public abstract class BaseModuleDeclSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : MemberSyntax(kind, children)
{
    public PathSyntax Name => Children.FirstOfType<PathSyntax>();
}