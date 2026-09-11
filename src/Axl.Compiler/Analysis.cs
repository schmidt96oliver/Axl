using System.Diagnostics;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Text;

namespace Axl.Compiler;

/// <summary>
/// Provides a query interface over the data structures provided
/// by a single <see cref="Compilation"/>. 
/// </summary>
public sealed class Analysis
{
    private readonly Compilation _compilation;
    
    internal Analysis(Compilation compilation)
    {
        _compilation = compilation;
    }

    
    /// <summary>
    /// The bottom-most <see cref="SyntaxNode"/> that contains the given
    /// <paramref name="location"/>.
    /// </summary>
    public SyntaxNode SyntaxNodeAt(SourceLocation location)
    {
        SyntaxNode currentNode = _compilation.SyntaxTree.FileSyntax;
        Debug.Assert(currentNode.Span?.Contains(location.Span) == true);

        while (true)
        {
            var nextNode = currentNode
                .SyntaxNodes()
                .SingleOrDefault(child => child.Span?.Contains(location.Span) == true);

            if (nextNode is null)
                return currentNode;

            currentNode = nextNode;
        }
    }
    
    
    public TypeSymbol? TypeOf(ExprSyntax syntax)
    {
        Debug.Assert(syntax.Span is not null);
        
        // Descend into bound tree to find expr syntax
        BoundStmt current = _compilation.BoundFile.Body;
        Debug.Assert(current.Syntax.Span?.Contains(syntax.Span.Value) == true);
        
        while (true)
        {
            var next = current.Children.FirstOrDefault(child => child.Syntax.Span?.Contains(syntax.Span.Value) == true);
            if (next is null) return null;
            if (next.Syntax == syntax)
            {
                Debug.Assert(next is BoundExpr);
                return ((BoundExpr)next).Type;
            }
        
            current = next;
        }
    }
}