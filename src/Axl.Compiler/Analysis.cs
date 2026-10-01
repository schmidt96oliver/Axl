using System.Collections.Frozen;
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
    private FrozenDictionary<ExprSyntax, TypeSymbol>? _typesBySyntax;
    
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

    private FrozenDictionary<ExprSyntax, TypeSymbol> BuildTypeTable()
    {
        var typesBySyntax = new Dictionary<ExprSyntax, TypeSymbol>();

        if (_compilation.BoundFile.ScriptFun.Body is BoundBlock scriptBlock)
            Recursive(scriptBlock);

        return typesBySyntax.ToFrozenDictionary();
        
        void Recursive(BoundStmt stmt)
        {
            if (stmt is BoundBlock block)
            {
                foreach (var fun in block.LocalFuns)
                {
                    if (fun.Body is BoundBlock funBlock)
                        Recursive(funBlock);
                }
            }
            
            if (stmt is BoundExpr { Syntax: ExprSyntax exprSyntax } expr)
                typesBySyntax.Add(exprSyntax, expr.Type);
            
            foreach (var child in stmt.Children)
                Recursive(child);
        }
    }
    
    public TypeSymbol? TypeOf(ExprSyntax syntax)
    {
        _typesBySyntax ??= BuildTypeTable();
        return _typesBySyntax.GetValueOrDefault(syntax);
    }
}