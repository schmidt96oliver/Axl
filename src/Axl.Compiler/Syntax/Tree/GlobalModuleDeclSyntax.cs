using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class GlobalModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.GlobalModuleDecl, children)
{
    public PathSyntax Name => Children.FirstOfType<PathSyntax>();
}