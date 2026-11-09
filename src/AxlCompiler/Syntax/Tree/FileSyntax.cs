using System.Collections.Immutable;
using System.Diagnostics;

namespace AxlCompiler.Syntax.Tree;

public sealed class FileSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.File, children)
{
    public override SyntaxTree Tree
    {
        get
        {
            Debug.Assert(field is not null, "Tree has not been set during construction.");
            return field;
        }
        internal set
        {
            Debug.Assert(field is null, "Tree has already been set.");
            field = value;
        }
    }
    
    public NamespaceDeclSyntax? NamespaceDecl => Children.FirstOfTypeOrNull<NamespaceDeclSyntax>();
    
    public IEnumerable<UsingDirectiveSyntax> UsingDirectives => Children.OfType<UsingDirectiveSyntax>();
}