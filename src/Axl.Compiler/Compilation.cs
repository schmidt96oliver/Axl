using System.Collections.Immutable;
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

    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public TypeContext TypeContext { get; }

    public GlobalSymbol GlobalSymbol
        => _lazyGlobalSymbol.GetOrCreate(CreateGlobalSymbol);    

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
    
    private ModuleFragment? GetModuleFragment(SyntaxTree syntaxTree)
    {
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
        
        return ModuleFragment.FromDeclaration(moduleDeclSyntax);
    }
    

    private ImmutableArray<Diagnostic> CollectDiagnostics()
    {
        var bag = new DiagnosticBag();

        foreach (var tree in SyntaxTrees)
            bag.AddRange(tree.Diagnostics);

        GlobalSymbol.CollectDiagnosticsInto(bag);

        return bag.Drain();
    }
}