using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : BaseModuleDeclSyntax(SyntaxKind.ModuleDecl, children)
{
    public PathSyntax Name => Children.FirstOfType<PathSyntax>();

    public IEnumerable<MemberSyntax> Members
        => Children.OfType<MemberSyntax>();
    
    public IEnumerable<UsingDirectiveSyntax> Usings
        => Children.OfType<UsingDirectiveSyntax>();   
}