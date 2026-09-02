using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ModuleDecl, children)
{
    public PathSyntax Path => Children.FirstOfType<PathSyntax>();
}