using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
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
    private enum BuildingContext
    {
        GlobalInModuleFile,
        GlobalInScriptFile,
        InModule
    }
    
    
    private readonly Compilation _compilation;

    private readonly Dictionary<MemberSyntax, Symbol> _symbolsBySyntax = [];
    private readonly List<Symbol> _topLevelSymbols = [];
    private readonly DiagnosticBag _diagnosticBag = new();

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
            [.. builder._topLevelSymbols],
            builder._diagnosticBag.Drain(),
            builder._diagnosticBag.HasError);
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
        AddSymbol(symbol);
        
        // Build children
        var children = decl.Children.Select(childDecl => BuildModuleSymbol(childDecl, symbol)).ToImmutableArray();
        symbol.ModuleMembers = children;

        return symbol;
    }


    private void AddSymbol(Symbol symbol)
    {
        foreach (var syntax in symbol.GetDeclaringSyntaxes())
        {
            Debug.Assert(!_symbolsBySyntax.ContainsKey((MemberSyntax)syntax));
            _symbolsBySyntax.Add((MemberSyntax)syntax, symbol);
        }
        
        if (symbol.Parent is null)
            _topLevelSymbols.Add(symbol);
    }
    
    private void BuildSyntaxTree(SyntaxTree syntaxTree)
    {
        foreach (var member in syntaxTree.FileSyntax.Members)
        {
            Build(member,
                parent: null,
                context: syntaxTree.GetAxlFileKind() is AxlFileKind.ModuleFile
                    ? BuildingContext.GlobalInModuleFile
                    : BuildingContext.GlobalInScriptFile);
        }
    }

    
    private void Build(MemberSyntax syntax, Symbol? parent, BuildingContext context)
    {
        switch (syntax)
        {
            case FnDeclSyntax fnDecl:
                BuildFn(fnDecl, parent, context);
                break;

            case ModuleDeclSyntax moduleDeclSyntax:
            {
                BuildModule(moduleDeclSyntax, context);
                break;
            }
            
            case NativeFnDeclSyntax nativeFnDecl:
            {
                BuildNativeFn(nativeFnDecl, parent, context);
                break;
            }
                
            case FileScopedModuleDeclSyntax fileScopedModuleDecl:
            {
                BuildFileScopedModule(fileScopedModuleDecl, parent, context);
                break;
            }
            
            default:
                throw new NotImplementedException();
        }
    }

    
    private void AnalyzeModifiers(MemberSyntax syntax, BuildingContext context)
    {
        //TODO: Implement Modifiers
        foreach (var modifier in syntax.Modifiers)
            _diagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(modifier));
    }
    
    private void BuildModule(ModuleDeclSyntax syntax, BuildingContext context)
    {
        if (context is not (BuildingContext.GlobalInModuleFile or BuildingContext.InModule))
        {
            _diagnosticBag.ReportError(new Diagnostic.NotAllowedInFileKind(syntax));
            return;
        }
        
        AnalyzeModifiers(syntax, context);
        
        // Already built eagerly. Just assert.
        Debug.Assert(_symbolsBySyntax[syntax] is ModuleSymbol);
        var moduleSymbol = (ModuleSymbol)_symbolsBySyntax[syntax];
                
        foreach (var member in syntax.Members)
            Build(member, 
                parent: moduleSymbol,
                BuildingContext.InModule);
    }

    private void BuildFn(FnDeclSyntax syntax, Symbol? parent, BuildingContext context)
    {
        if (context is not (BuildingContext.GlobalInScriptFile or BuildingContext.InModule))
        {
            _diagnosticBag.ReportError(new Diagnostic.NotAllowedInFileKind(syntax));
            return;
        }
     
        AnalyzeModifiers(syntax, context);
        
        var fnSymbol = new FnSymbol(_compilation,
            SymbolName.From(syntax.Name),
            syntax,
            parent);
                
        AddSymbol(fnSymbol);
    }
    
    
    private void BuildFileScopedModule(FileScopedModuleDeclSyntax syntax, Symbol? parent, BuildingContext context)
    {
        // if (context is not (BuildingContext.GlobalInModuleFile or BuildingContext.InModule))
        // {
        //     _diagnosticBag.ReportError();
        //     return;
        // }
        
        //TODO: Implement file scoped module decl
                
        _diagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(syntax));
        var errorSymbol = new ErrorSymbol(_compilation, SymbolName.From(syntax.Name.Parts.Last()), syntax, parent);
                
        AddSymbol(errorSymbol);
    }

    private void BuildNativeFn(NativeFnDeclSyntax syntax, Symbol? parent, BuildingContext context)
    {
        // if (context is not (BuildingContext.GlobalInScriptFile or BuildingContext.InModule))
        // {
        //     _diagnosticBag.ReportError();
        //     return;
        // }
        
        //TODO: Implement native fn decl
                
        _diagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(syntax));
        var errorSymbol = new ErrorSymbol(_compilation, SymbolName.From(syntax.Name), syntax, parent);
                
        AddSymbol(errorSymbol);
    }
}