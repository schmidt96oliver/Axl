using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Declarations;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler;

public class Compilation
{
    private LazyField<ModuleSymbol> _lazyGlobalModule;
    private LazyField<ImmutableArray<Diagnostic>> _lazyDiagnostics;

    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public DeclarationTable DeclarationTable { get; }
    public TypeContext TypeContext { get; }

    public ModuleSymbol GlobalModule
        => _lazyGlobalModule.GetOrCreate(() =>
            new ModuleSymbol(this, DeclarationTable.GlobalDecl, parent: null));

    public ImmutableArray<Diagnostic> Diagnostics
        => _lazyDiagnostics.GetOrCreate(CollectDiagnostics);


    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees)
    {
        SyntaxTrees = syntaxTrees;
        TypeContext = new TypeContext();
        DeclarationTable = new DeclarationTable(syntaxTrees);
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



    private ImmutableArray<Diagnostic> CollectDiagnostics()
    {
        var bag = new DiagnosticBag();

        foreach (var tree in SyntaxTrees)
            bag.AddRange(tree.Diagnostics);

        GlobalModule.CollectDiagnosticsInto(bag);

        return bag.Drain();
    }
}