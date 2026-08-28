using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// Creates <see cref="Symbol"/>s for member declarations.
/// Only creates globally visible declared symbols; does not go
/// into bodies.
/// </summary>
/// <remarks>
/// First, creates a table of all module declarations by their path. Then
/// eagerly creates all symbols by walking all syntax trees.
/// </remarks>
public partial class SymbolTableBuilder
{
    private readonly Compilation _compilation;

    private readonly Dictionary<MemberSyntax, Symbol> _symbolsBySyntax = [];
    private readonly List<Symbol> _topLevelSymbols = [];

    private SymbolTableBuilder(Compilation compilation)
    {
        _compilation = compilation;
    }
    
    
    public static SymbolTable Build(Compilation compilation)
    {
        var builder = new SymbolTableBuilder(compilation);
        
        builder.BuildModuleSymbols();
        foreach (var syntaxTree in compilation.SyntaxTrees)
            builder.BuildSyntaxTree(syntaxTree);
        
        return new SymbolTable(
            builder._symbolsBySyntax.ToFrozenDictionary(),
            [.. builder._topLevelSymbols]);
    }

    
    /// <summary>
    /// Eagerly builds all module symbols before walking the tree.
    /// Can't be built during tree-walking, because we need to attach
    /// all module-children eagerly. See also <see cref="ModuleSymbol.ModuleMembers"/>.
    /// </summary>
    private void BuildModuleSymbols()
    {
        var topLevelDecls = ModuleDeclBuilder.Build(_compilation.SyntaxTrees);
        
        foreach (var moduleDecl in topLevelDecls)
            BuildModuleSymbol(moduleDecl, parent: null);
    }

    private ModuleSymbol BuildModuleSymbol(ModuleDecl decl, ModuleSymbol? parent)
    {
        var symbol = new ModuleSymbol(
            _compilation,
            decl.Name,
            [..decl.Syntaxes],
            parent
        );
        
        // Add references
        if (parent is null)
            _topLevelSymbols.Add(symbol);
        foreach (var syntax in decl.Syntaxes)
            _symbolsBySyntax.Add(syntax, symbol);
        
        // Build children
        var children = decl.Children.Select(childDecl => BuildModuleSymbol(childDecl, symbol)).ToImmutableArray();
        symbol.ModuleMembers = children;

        return symbol;
    }
    
    
    private void BuildSyntaxTree(SyntaxTree syntaxTree)
    {
        foreach (var member in syntaxTree.FileSyntax.Members)
        {
            Build(member, parent: null);
        }
    }

    private void Build(MemberSyntax syntax, Symbol? parent)
    {
        switch (syntax)
        {
            case FnDeclSyntax fnDecl:
                if (fnDecl.Name.IsMissing)
                    break;

                var fnSymbol = new FnSymbol(_compilation,
                    SymbolName.From(fnDecl.Name),
                    fnDecl,
                    parent);
                _symbolsBySyntax.Add(fnDecl, fnSymbol);
                if (parent is null)
                    _topLevelSymbols.Add(fnSymbol);
                
                break;

            case ModuleDeclSyntax moduleDeclSyntax:
            {
                // Already built eagerly. Just assert.
                Debug.Assert(_symbolsBySyntax[moduleDeclSyntax] is ModuleSymbol);
                var moduleSymbol = (ModuleSymbol)_symbolsBySyntax[moduleDeclSyntax];
                
                foreach (var member in moduleDeclSyntax.Members)
                    Build(member, parent: moduleSymbol);
                break;
            }
            
            default:
                //TODO: NativeFnDecl
                //TODO: FileScopeModuleDecl
                throw new NotImplementedException();
        }
    }
}