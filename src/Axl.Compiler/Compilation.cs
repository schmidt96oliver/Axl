using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler;

//TODO: Cycle detection??
public class Compilation
{
    public enum QueryKind
    {
        GetSymbolTable,
        GetType,
    }
    public readonly record struct QueryKey(QueryKind Kind, object? Data = null);

    private readonly record struct QueryHandle(QueryKey Key, Compilation Compilation) : IDisposable
    {
        public void Dispose()
        {
            var poppedKey = Compilation._activeQueries.Pop();
            Debug.Assert(poppedKey == Key);
        }
    }
    
    private sealed class SyntaxTreeTable<T> : Dictionary<SyntaxTree, T>;

    
    private SymbolTable? _symbolTable = null;

    private Stack<QueryKey> _activeQueries = [];
    
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


    private QueryHandle ProtectedQuery(QueryKind query, object? data = null)
    {
        var key = new QueryKey(query, data);
        if (_activeQueries.Contains(key))
            throw new CyclicQueryException(_activeQueries, key);
        
        _activeQueries.Push(key);
        return new QueryHandle(key, this);
    }
    
    public SymbolTable GetSymbolTable()
    {
        if (_symbolTable is null)
        {
            using (ProtectedQuery(QueryKind.GetSymbolTable)) 
                _symbolTable = Declarator.GetSymbolTable(this, SyntaxTrees);
        }

        return _symbolTable;
    }

}

public class CyclicQueryException(Stack<Compilation.QueryKey> activeQuery, Compilation.QueryKey offendingQuery) 
    : Exception(GetMessage(activeQuery, offendingQuery))
{
    private static string GetMessage(Stack<Compilation.QueryKey> queries, Compilation.QueryKey offendingQuery)
    {
        var stack = string.Join("\n", queries.Select(QueryToString));
        return $"Compilation queries were cyclic at query \"{QueryToString(offendingQuery)}\". Active queries: \n{stack}";

        string QueryToString(Compilation.QueryKey query)
            => $"{query.Kind}" + (query.Data is not null ? $" {query.Data}" : "");
    }
}