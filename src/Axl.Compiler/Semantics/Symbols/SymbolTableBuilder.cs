using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// Maps member declaration syntax to their symbol while creating those
/// symbols. Only creates globally visible declared symbols; does not go
/// into bodies.
/// </summary>
/// <remarks>
/// First, creates a table of all module declarations by their path. Then
/// eagerly creates all symbols by walking all syntax trees.
/// </remarks>
public class SymbolTableBuilder
{
    private sealed class ModuleDeclTable : Dictionary<SymbolPath, List<ModuleDeclSyntax>>;
    
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
        var moduleDeclTable = GetModuleDeclTable(compilation.SyntaxTrees);

        var builder = new SymbolTableBuilder(compilation, moduleDeclTable);
        foreach (var syntaxTree in compilation.SyntaxTrees)
            builder.BuildSyntaxTree(syntaxTree);
        
        return new SymbolTable(compilation,
            builder._symbolsBySyntax.ToFrozenDictionary(),
            builder._allSymbols);
    }

    private static ModuleDeclTable GetModuleDeclTable(ImmutableArray<SyntaxTree> trees)
    {
        var decls = new ModuleDeclTable();
        
        foreach (var tree in trees)
        foreach (var moduleDecl in tree.FileSyntax.Children.OfType<ModuleDeclSyntax>())
            VisitModuleSyntax(parentPath: null, moduleDecl);

        return decls;

        void VisitModuleSyntax(SymbolPath? parentPath, ModuleDeclSyntax syntax)
        {
            var path = GetModuleDeclSyntaxPath(syntax, parentPath);
            if (path is null)
            {
                // The path was invalid. Discard everything else.
                return;
            }
            
            decls.TryAdd(path.Value, []);
            decls[path.Value].Add(syntax);

            foreach (var childModuleSyntax in syntax.Members.OfType<ModuleDeclSyntax>())
                VisitModuleSyntax(path, childModuleSyntax);
        }

        
    }

    /// <summary>
    /// Appends path name of a module syntax to the parent path. Returns
    /// null, if one or more path identifiers are missing.
    /// </summary>
    private static SymbolPath? GetModuleDeclSyntaxPath(ModuleDeclSyntax syntax, SymbolPath? parentPath)
    {
        var parts = ImmutableArray.CreateBuilder<SymbolName>();
        if (parentPath is SymbolPath actualParentPath)
            parts.AddRange(actualParentPath.Parts);

        foreach (var partIdToken in syntax.Name.Parts)
        {
            if (partIdToken.IsMissing)
                return null;
                
            parts.Add(SymbolName.From(partIdToken));
        }

        return SymbolPath.From(parts.DrainToImmutable());
    }
    
    
    private ModuleSymbol GetOrCreateModuleSymbolByPath(SymbolPath path)
    {
        // Find module symbol
        var moduleSymbol = _allSymbols
            .OfType<ModuleSymbol>()
            .FirstOrDefault(symbol => symbol.Path == path);
        if (moduleSymbol is not null)
            return moduleSymbol;

        // Build it
        var parentPath = path.GetParentPath();
        var parent = parentPath is not null ? GetOrCreateModuleSymbolByPath(parentPath.Value) : null;
        var syntaxes = _moduleDeclTable.GetValueOrDefault(path) ?? [];

        moduleSymbol = new ModuleSymbol(_compilation,
            SymbolName.From(path.LastPart),
            [.. syntaxes],
            parent);

        // Add references
        _allSymbols.Add(moduleSymbol);
        foreach (var declSyntax in syntaxes)
            _symbolsBySyntax.Add(declSyntax, moduleSymbol);

        return moduleSymbol;
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