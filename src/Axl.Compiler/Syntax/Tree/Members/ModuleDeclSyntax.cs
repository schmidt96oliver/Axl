using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberDeclSyntax(SyntaxKind.ModuleDecl, children)
{
    public QualifiedNameSyntax Name => NthChildOfType<QualifiedNameSyntax>(0);

    public IEnumerable<MemberDeclSyntax> Members
        => Children.OfType<MemberDeclSyntax>();
}