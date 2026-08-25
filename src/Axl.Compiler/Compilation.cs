using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler;

public class Compilation
{
    private sealed class SyntaxTreeTable<T> : Dictionary<SyntaxTree, T>;

    private SymbolTable? _symbolTable = null;
    
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees)
    {
        SyntaxTrees = syntaxTrees;
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


    public SymbolTable GetSymbolTable()
    {
        _symbolTable ??= Declarator.GetSymbolTable(this, SyntaxTrees);
        return _symbolTable;
    }
}