using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using Binder = Axl.Compiler.Binding.Binder;

namespace Axl.Compiler;

public class Compilation
{
    private readonly Dictionary<SyntaxTree, ScriptSymbol> _boundScripts = [];
    
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (field.IsDefault)
                Complete();
            return field;
        }
        private set;
    }

    public SemanticSideTable SemanticSideTable
    {
        get
        {
            if (field is null)
                Complete();
            return field!;
        }
        private set;
    }

    public ImmutableArray<ScriptSymbol> Scripts
    {
        get
        {
            if (field.IsDefault)
                field = [.. SyntaxTrees.Select(GetBoundScript)];
            return field;
        }
    }

    public RootNamespaceSymbol RootNamespace { get; }


    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees)
    {
        SyntaxTrees = syntaxTrees;
        RootNamespace = new RootNamespaceSymbol();
    }

    public static Compilation From(params IEnumerable<SourceText> texts)
    {
        var trees = texts.Select(Parser.Parse).ToImmutableArray();
        return new Compilation(trees);
    }


    private void Complete()
    {
        var sideEffects = new BinderSideEffects();
        foreach (var script in Scripts)
            script.Complete(sideEffects);

        Diagnostics = [.. SyntaxTrees.SelectMany(tree => tree.Diagnostics), ..sideEffects.DiagnosticBag.Drain()];
        SemanticSideTable = sideEffects.SemanticSideTable;
    }
    

    public ScriptSymbol GetBoundScript(SyntaxTree tree)
    {
        if (!_boundScripts.TryGetValue(tree, out var boundFile))
        {
            boundFile = Binder.BindScriptFile(tree, RootNamespace);
            _boundScripts.Add(tree, boundFile);
        }

        return boundFile;
    }

    public SyntaxTree GetSyntaxTree(SourceText text)
    {
        return SyntaxTrees.FirstOrDefault(tree => tree.SourceText == text)
               ?? throw new ArgumentException($"Syntax tree for given {nameof(text)} does not exit.", nameof(text));
    }


    /// <summary>
    /// The bottom-most <see cref="SyntaxNode"/> that contains the given
    /// <paramref name="location"/>.
    /// </summary>
    public SyntaxNode SyntaxNodeAt(SourceLocation location)
    {
        SyntaxNode currentNode = GetSyntaxTree(location.SourceText).FileSyntax;
        Debug.Assert(currentNode.FullRange.Contains(location.Range));

        while (true)
        {
            var nextNode = currentNode
                .SyntaxNodes()
                .SingleOrDefault(child => child.Range?.Contains(location.Range) == true);

            if (nextNode is null)
                return currentNode;

            currentNode = nextNode;
        }
    }

    public Token TokenAt(SourceLocation location)
    {
        SyntaxElement current = GetSyntaxTree(location.SourceText).FileSyntax;
        Debug.Assert(current.FullRange.Contains(location.Range));

        while (true)
        {
            if (current is Token token)
            {
                Debug.Assert(token.FullRange.Contains(location.Range));
                return token;
            }
            
            var nextNode = (current as SyntaxNode)!
                .Children
                .Single(child => child.Range?.Contains(location.Range) == true);

            current = nextNode;
        }
    }
}