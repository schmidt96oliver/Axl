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
        Debug.Assert(currentNode.FullRange.Contains(location.Range));

        while (true)
        {
            var nextNode = currentNode
                .SyntaxNodes()
                .SingleOrDefault(child => child.Range?.Contains(location.Range) == true);

            if (nextNode is null)
                return currentNode;

            currentNode = nextNode;
        }
    }
    
    
    public TypeSymbol? TypeOf(ExprSyntax syntax)
    {
        Debug.Assert(syntax.Range is not null);
        var syntaxRange = syntax.Range.Value;
        
        //TODO: Account for nested funs
        // When local funs are added, they will nest into each other
        // and we need to find the deepest, because normally, the blocks range
        // will cover the local fun.
        
        // Find the bound block it is inside.
        var owningBlock = _compilation.BoundFile.Funs
            .Where(fun => fun.Body is BoundBlock)
            .Select(fun => (fun.Body.Value as BoundBlock)!)
            .FirstOrDefault(block => block.Syntax.Location.Range.Contains(syntaxRange));

        if (owningBlock is null)
        {
            if (_compilation.BoundFile.ScriptFun.Body is not BoundBlock scriptBlock)
                return null;
            owningBlock = scriptBlock;
        }
        
        // Descend into bound tree to find expr syntax
        BoundStmt current = owningBlock;
        Debug.Assert(current.Syntax.Range?.Contains(syntaxRange) == true);
        
        while (true)
        {
            var next = current.Children.FirstOrDefault(child => child.Syntax.Range?.Contains(syntaxRange) == true);
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