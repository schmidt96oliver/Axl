using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler;

public class Compilation
{
    private LazyField<ImmutableArray<Diagnostic>> _lazyDiagnostics;
    private LazyField<GlobalSymbol> _lazyGlobalSymbol;
    private LazyField<ImmutableArray<ScriptSymbol>> _lazyScriptSymbols;

    private readonly Dictionary<SyntaxTree, ModuleFragment?> _moduleFragmentByTree = [];
    private readonly Dictionary<SyntaxNode, Symbol?> _globalSymbolsBySyntax = [];
    
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public TypeContext TypeContext { get; }

    public GlobalSymbol GlobalSymbol
        => _lazyGlobalSymbol.GetOrCreate(CreateGlobalSymbol);

    public ImmutableArray<ScriptSymbol> ScriptSymbols
        => _lazyScriptSymbols.GetOrCreate(CreateScriptSymbols);

    public ImmutableArray<Diagnostic> Diagnostics
        => _lazyDiagnostics.GetOrCreate(CollectDiagnostics);


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

    public static Compilation FromText(string sourceText)
    {
        var tree = Parser.Parse(SourceFileView.FromText(sourceText));
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
                .Select(tree => new ScriptSymbol(compilation: this, syntax: tree.FileSyntax))
        ];
    
    
    private ModuleFragment? GetModuleFragment(SyntaxTree syntaxTree)
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

    private ModuleSymbol GetModuleSymbol(ModuleFragment fragment, ModuleSymbol? parent)
    {
        var symbol = parent?.Members.OfType<ModuleSymbol>().Single(module => module.Fragments.Contains(fragment))
                     ?? GlobalSymbol.Members.OfType<ModuleSymbol>().Single(module => module.Fragments.Contains(fragment));

        return fragment is ModuleFragment.Prefix(_, var child)
            ? GetModuleSymbol(child, symbol)
            : symbol;
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
                    var fragment = GetModuleFragment(fileSyntax.Tree);
                    if (fragment is null)
                        return null;

                    return GetModuleSymbol(fragment, parent: null);
                }

                // A module declares it's files module -or- nothing
                // if it was in an invalid position.
                case ModuleDeclSyntax:
                {
                    var fragment = GetModuleFragment(syntax.Tree);
                    if (fragment is null)
                        return null;

                    if (fragment.GetBody().Syntax == syntax)
                        return GetModuleSymbol(fragment, parent: null);

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

    
    private ImmutableArray<Diagnostic> CollectDiagnostics()
    {
        var bag = new DiagnosticBag();

        foreach (var tree in SyntaxTrees)
            bag.AddRange(tree.Diagnostics);

        GlobalSymbol.CollectDiagnosticsInto(bag);
        
        foreach (var script in ScriptSymbols)
            script.CollectDiagnosticsInto(bag);

        return bag.Drain();
    }
}