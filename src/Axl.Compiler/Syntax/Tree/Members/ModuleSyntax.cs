using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ModuleSyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.ModuleDecl, children)
{
    public PathSyntax Name => NthChildOfType<PathSyntax>(0);

    public IEnumerable<MemberSyntax> Members
        => Children.OfType<MemberSyntax>();
}