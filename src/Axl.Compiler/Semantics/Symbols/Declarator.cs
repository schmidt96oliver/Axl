using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// Creates member symbols in two phases.
/// First collects module declarations across files.
/// Second creates all symbols.
/// </summary>
public class Declarator
{
    private readonly record struct SingleModuleDecl(string Path, ModuleDeclSyntax Syntax);

    private sealed class SingleModuleDeclTable : Dictionary<string, List<ModuleDeclSyntax>>;
    
    public static SymbolTable GetSymbolTable(Compilation compilation, ImmutableArray<SyntaxTree> trees)
    {
        var singleModuleDecls = GetModuleDeclTable(trees);
        var table = BuildSymbolTable(compilation, trees, singleModuleDecls);
        return table;
    }


    private static SingleModuleDeclTable GetModuleDeclTable(ImmutableArray<SyntaxTree> trees)
    {
        var decls = new SingleModuleDeclTable();
        
        foreach (var tree in trees)
        foreach (var moduleDecl in tree.FileSyntax.Children.OfType<ModuleDeclSyntax>())
            VisitModuleSyntax(parentPath: "", moduleDecl);

        return decls;

        void VisitModuleSyntax(string parentPath, ModuleDeclSyntax syntax)
        {
            var path = string.Join(".", syntax.Name.Parts.Select(part => part.Identifier));
            if (parentPath.Length > 0)
                path = string.Join(".", parentPath, path);
            
            decls.TryAdd(path, []);
            decls[path].Add(syntax);

            foreach (var childModuleSyntax in syntax.Members.OfType<ModuleDeclSyntax>())
                VisitModuleSyntax(path, childModuleSyntax);
        }
    }

    
    private static SymbolTable BuildSymbolTable(Compilation compilation, ImmutableArray<SyntaxTree> trees,
        SingleModuleDeclTable moduleDecls)
    {
        // Build all module symbols
        Dictionary<SyntaxNode, Symbol> symbols = [];
        HashSet<Symbol> allSymbols = [];

        Dictionary<string, ModuleSymbol> modulesByPath = [];
        foreach (var pathToModuleDecl in moduleDecls)
        {
            var moduleSymbol = GetModuleByPath(pathToModuleDecl.Key);
            
            foreach (var moduleSyntax in pathToModuleDecl.Value)
                symbols.Add(moduleSyntax, moduleSymbol);
        }
        
        // Create each fn symbol
        foreach (var tree in trees)
        foreach (var symbol in VisitNode(compilation, symbols, tree.FileSyntax, null))
        {
            var fnSymbol = (FnSymbol)symbol;
            allSymbols.Add(fnSymbol);
            symbols.Add(fnSymbol.Syntax, fnSymbol);
        }
        
        return new SymbolTable(compilation, symbols.ToFrozenDictionary(), allSymbols);

        ModuleSymbol GetModuleByPath(string path)
        {
            if (modulesByPath.TryGetValue(path, out var moduleSymbol))
                return moduleSymbol;

            var parentPath = path.Contains('.') ? path[..path.LastIndexOf('.')] : "";
            var syntaxes = moduleDecls.GetValueOrDefault(path) ?? [];
            
            moduleSymbol = new ModuleSymbol(compilation,
                SymbolName.From(path),
                syntaxes.ToImmutableArray(),
                Parent: parentPath != "" ? GetModuleByPath(parentPath) : null);
            
            modulesByPath.Add(path, moduleSymbol);
            allSymbols.Add(moduleSymbol);
            
            return moduleSymbol;
        }
    }

    private static IEnumerable<Symbol> VisitNode(Compilation compilation, Dictionary<SyntaxNode, Symbol> symbols,
        SyntaxNode node, Symbol? parent)
    {
        switch (node)
        {
            case FileSyntax fileSyntax:
                foreach (var member in fileSyntax.Members)
                foreach (var symbol in VisitNode(compilation, symbols, member, parent))
                    yield return symbol;
                break;
            
            case ModuleDeclSyntax moduleDecl:
                var moduleSymbol = symbols[moduleDecl];
                foreach (var member in moduleDecl.Members)
                foreach (var symbol in VisitNode(compilation, symbols, member, parent: moduleSymbol))
                    yield return symbol;
                break;
            
            case FnDeclSyntax fnDecl:
                var fnSymbol = new FnSymbol(compilation, SymbolName.From(fnDecl.Name), fnDecl, parent);
                yield return fnSymbol;
                break;
        }
    }
}