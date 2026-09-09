using System.Diagnostics;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler;

/// <summary>
/// Provides a query interface over the data structures provided
/// by a single <see cref="Compilation"/>. Answers to queries are
/// cached.
/// </summary>
public sealed class Analysis
{
    private readonly Compilation _compilation;
    
    private readonly Dictionary<SyntaxNode, Symbol?> _globalSymbolsBySyntax = [];
    

    internal Analysis(Compilation compilation)
    {
        _compilation = compilation;
    }

    
    public SyntaxTree SyntaxTreeAt(SourceLocation location)
        => _compilation.SyntaxTrees.SingleOrDefault(tree =>
               tree.Source.File == location.File &&
               tree.FileSyntax.Span?.Contains(location.Span) == true)
           ?? throw new ArgumentException($"{nameof(location)} does not belong to this compilation.");
    
    /// <summary>
    /// The bottom-most <see cref="SyntaxNode"/> that contains the given
    /// <paramref name="location"/>.
    /// </summary>
    public SyntaxNode SyntaxNodeAt(SourceLocation location)
    {
        var tree = SyntaxTreeAt(location);

        SyntaxNode currentNode = tree.FileSyntax;
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
    
    
    /// <summary>
    /// Finds the <see cref="Symbol"/> which is globally visible and declared
    /// by <paramref name="syntax"/>. Can only find symbols, which are part of
    /// a module file. Returns <c>null</c>, if the <paramref name="syntax"/> does
    /// not declare a globally visible symbol.
    /// <para>
    /// For <see cref="FileSyntax"/>, returns the <see cref="ModuleSymbol"/> declared
    /// by that file or <c>null</c>, if it doesn't declare a module.
    /// </para>
    /// </summary>
    public Symbol? GetGloballyDeclaredSymbol(SyntaxNode syntax)
    {
        if (_globalSymbolsBySyntax.TryGetValue(syntax, out var symbol))
            return symbol;

        symbol = Find(syntax);
        _globalSymbolsBySyntax.Add(syntax, symbol);
        return symbol;
        
        Symbol? Find(SyntaxNode syntax)
        {
            switch (syntax)
            {
                // A file declares it's module or nothing, when it
                // is a script file.
                case FileSyntax fileSyntax:
                {
                    var fragment = _compilation.GetModuleFragment(fileSyntax.Tree);
                    if (fragment is null)
                        return null;

                    return _compilation.GetModuleSymbol(fragment, parent: null);
                }

                // A module declares it's files module -or- nothing
                // if it was in an invalid position.
                case ModuleDeclSyntax:
                {
                    var fragment = _compilation.GetModuleFragment(syntax.Tree);
                    if (fragment is null)
                        return null;

                    if (fragment.GetBody().Syntax == syntax)
                        return _compilation.GetModuleSymbol(fragment, parent: null);

                    return null;
                }
            
                case MemberSyntax:
                {
                    Debug.Assert(syntax.Parent is not null, "Members always have a parent.");
                
                    var parentSymbol = GetGloballyDeclaredSymbol(syntax.Parent!);
                    if (parentSymbol is ModuleSymbol parentModule)
                        return parentModule.Members.Single(member => member.DeclaringSyntaxes.Contains(syntax));

                    return null;
                }
                
                default:
                    return null;
            }
        }
    }


    public Hir HirFor(SyntaxNode syntax)
    {
        var script = _compilation.ScriptSymbols.SingleOrDefault(script => script.FileSyntax == syntax.Tree.FileSyntax);
        if (script is null)
            //TODO: Implement fns and local fns
            throw new NotImplementedException("Cannot search on fn bodies yet.");

        return _compilation.Bind(script);
    }

    public AxlType? TypeOf(ExprSyntax syntax)
    {
        var hir = HirFor(syntax);
        
        // Descend into hir tree to find expr syntax
        
        throw new NotImplementedException();
    }
}