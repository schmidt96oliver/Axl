using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Declarations;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler;

public partial class Compilation
{
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    public DeclarationTable DeclarationTable
    {
        get
        {
            field ??= new DeclarationTable(SyntaxTrees);
            return field;
        }
    }
    
    public TypeContext TypeContext { get; set; }

    public ModuleSymbol GlobalModule
    {
        get
        {
            field ??= new ModuleSymbol(compilation: this,
                DeclarationTable.GlobalDecl,
                parent: null);
            return field;
        }
    }


    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (field.IsDefault)
                field = ProtectCycles(QueryKind.Compilation_Diagnostics, CollectDiagnostics);
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