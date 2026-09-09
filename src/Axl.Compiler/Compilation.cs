using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Binders;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Semantics.Scopes;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Taxl;

namespace Axl.Compiler;

public class Compilation
{
    private LazyField<ImmutableArray<Diagnostic>> _lazyDiagnostics;
    private LazyField<GlobalSymbol> _lazyGlobalSymbol;
    private LazyField<ImmutableArray<ScriptSymbol>> _lazyScriptSymbols;

    private readonly Dictionary<SyntaxTree, ModuleFragment?> _moduleFragmentByTree = [];
    private readonly Dictionary<ScriptSymbol, Hir> _hirByScript = [];
    private readonly Dictionary<FnSymbol, Hir> _hirByFn = [];
    
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public TypeContext TypeContext { get; }

    public GlobalSymbol GlobalSymbol
        => _lazyGlobalSymbol.GetOrCreate(CreateGlobalSymbol);

    public ImmutableArray<ScriptSymbol> ScriptSymbols
        => _lazyScriptSymbols.GetOrCreate(CreateScriptSymbols);

    public ImmutableArray<Diagnostic> Diagnostics
        => _lazyDiagnostics.GetOrCreate(CollectDiagnostics);

    public Analysis Analysis
    {
        get
        {
            field ??= new Analysis(this);
            return field;
        }
    }


    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees)
    {
        SyntaxTrees = syntaxTrees;
        TypeContext = new TypeContext();
    }

    
    public static Compilation FromFile(string path)
    {
        var tree = Parser.Parse(SourceFileView.FromFile(path));
        return new Compilation([tree]);
    }

    public static Compilation FromText(string path, string sourceText)
    {
        var sourceFile = SourceFile.FromText(path, sourceText);
        var tree = Parser.Parse(SourceFileView.Whole(sourceFile));
        return new Compilation([tree]);
    }

    public static Compilation FromTrees(params ReadOnlySpan<SyntaxTree> trees)
    {
        return new Compilation([.. trees]);
    }

    public static Compilation FromTrees(IEnumerable<SyntaxTree> trees)
    {
        return new Compilation([.. trees]);
    }

    public static Compilation From(TaxlFile taxlFile)
        => FromTrees(taxlFile.Fragments.Select(fragment => Parser.Parse(fragment.SourceView)));


    private GlobalSymbol CreateGlobalSymbol()
    {
        var fragments = SyntaxTrees
            .Select(GetModuleFragment)
            .Where(fragment => fragment is not null)
            .ToImmutableArray();
        return new GlobalSymbol(compilation: this, moduleFragments: fragments!);
    }

    private ImmutableArray<ScriptSymbol> CreateScriptSymbols()
        =>
        [
            .. SyntaxTrees
                .Where(tree => GetModuleFragment(tree) is null)
                .Select(tree => new ScriptSymbol(compilation: this, fileSyntax: tree.FileSyntax))
        ];
    
    
    public ModuleFragment? GetModuleFragment(SyntaxTree syntaxTree)
    {
        if (_moduleFragmentByTree.TryGetValue(syntaxTree, out var fragment))
            return fragment;
        
        // The first syntax that is not `using` will determine the kind of
        // file. Further module declarations are reported and then ignored.

        var firstNonUsingSyntax = syntaxTree.FileSyntax
            .SyntaxNodes()
            .FirstOrDefault(node => node is not UsingDirectiveSyntax);
        if (firstNonUsingSyntax is not ModuleDeclSyntax moduleDeclSyntax)
        {
            // It's not a module, which means we see it as a script file. It
            // will not contribute to global modules.
            return null;
        }
        
        fragment = ModuleFragment.FromDeclaration(moduleDeclSyntax);
        _moduleFragmentByTree.Add(syntaxTree, fragment);
        return fragment;
    }

    public ModuleSymbol GetModuleSymbol(ModuleFragment fragment, ModuleSymbol? parent)
    {
        var symbol = parent?.Members.OfType<ModuleSymbol>().Single(module => module.Fragments.Contains(fragment))
                     ?? GlobalSymbol.Members.OfType<ModuleSymbol>().Single(module => module.Fragments.Contains(fragment));

        return fragment is ModuleFragment.Prefix(_, var child)
            ? GetModuleSymbol(child, symbol)
            : symbol;
    }

    


    public Hir Bind(ScriptSymbol scriptSymbol)
    {
        if (_hirByScript.TryGetValue(scriptSymbol, out var hir))
            return hir;
        
        var globalScope = new GlobalScope(GlobalSymbol);
        var fileScope = new FileScope(scriptSymbol.FileSyntax, parent: globalScope);

        hir = Binder.Bind(scriptSymbol, enclosingScope: fileScope);
        _hirByScript.Add(scriptSymbol, hir);
        return hir;
    }

    public Hir Bind(FnSymbol fnSymbol)
    {
        throw new NotImplementedException();
    }
    
    
    private ImmutableArray<Diagnostic> CollectDiagnostics()
    {
        var bag = new DiagnosticBag();

        // --- Syntax
        foreach (var tree in SyntaxTrees)
            bag.AddRange(tree.Diagnostics);

        // --- Declarations
        GlobalSymbol.CollectDiagnosticsInto(bag);
        foreach (var script in ScriptSymbols)
            script.CollectDiagnosticsInto(bag);
        
        // --- HIR and local symbols
        foreach (var hir in ScriptSymbols.Select(Bind))
        {
            bag.AddRange(hir.Diagnostics);
            foreach (var localFn in hir.LocalMembers)
                localFn.CollectDiagnosticsInto(bag);
        }

        return bag.Drain();
    }
}