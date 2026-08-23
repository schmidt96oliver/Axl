using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FileScopedModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.FileScopedModuleDecl, children)
{
    public PathSyntax Name => Children.FirstOfType<PathSyntax>();
}