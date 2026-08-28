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
    private readonly ModuleDeclTable _moduleDeclTable;

    private readonly Dictionary<MemberSyntax, Symbol> _symbolsBySyntax = [];
    private readonly HashSet<Symbol> _allSymbols = [];

    private SymbolTableBuilder(Compilation compilation, ModuleDeclTable moduleDeclTable)
    {
        _compilation = compilation;
        _moduleDeclTable = moduleDeclTable;
    }
    
    
    public static SymbolTable Build(Compilation compilation)
    {
        var moduleDeclTable = ModuleDeclTable.Build(compilation.SyntaxTrees);
        var builder = new SymbolTableBuilder(compilation, moduleDeclTable);
        foreach (var syntaxTree in compilation.SyntaxTrees)
            builder.BuildSyntaxTree(syntaxTree);
        
        return new SymbolTable(compilation,
            builder._symbolsBySyntax.ToFrozenDictionary(),
            builder._allSymbols);
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
                break;

            case ModuleDeclSyntax moduleDeclSyntax:
            {
                // Check, if it has already been built.
                if (!_symbolsBySyntax.TryGetValue(moduleDeclSyntax, out var moduleSymbol))
                {
                    var path = GetModuleDeclSyntaxPath(moduleDeclSyntax, parent?.Path);
                    if (path is null)
                        break;
                    
                    moduleSymbol = GetOrCreateModuleSymbolByPath(path.Value);
                }
                
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