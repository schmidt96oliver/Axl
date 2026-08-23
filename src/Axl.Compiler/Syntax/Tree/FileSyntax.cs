using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FileSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.File, children)
{
    public IEnumerable<UsingDirectiveSyntax> Usings
        => Children.OfType<UsingDirectiveSyntax>();   
    
    public IEnumerable<MemberSyntax> Members 
        => Children.OfType<MemberSyntax>();
    
    public IEnumerable<StmtSyntax> Stmts 
        => Children.OfType<StmtSyntax>();
}