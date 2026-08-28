using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FileSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.File, children)
{
    private SyntaxTree? _tree;

    public override SyntaxTree Tree
    {
        get
        {
            Debug.Assert(_tree is not null, "Tree has not been set during construction.");
            return _tree;
        }
        internal set
        {
            Debug.Assert(_tree is null, "Tree has already been set.");
            _tree = value;
        }
    }

    public IEnumerable<UsingDirectiveSyntax> Usings
        => Children.OfType<UsingDirectiveSyntax>();   
    
    public IEnumerable<MemberSyntax> Members 
        => Children.OfType<MemberSyntax>();
    
    public IEnumerable<StmtSyntax> Stmts 
        => Children.OfType<StmtSyntax>();
}