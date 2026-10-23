using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using Binder = Axl.Compiler.Binding.Binder;

namespace Axl.Compiler;

public class Compilation
{
    public static string BaseLibraryDirectory
        => Path.Combine(AppContext.BaseDirectory, "BaseLib");
    
    
    /// <summary>
    /// All dependencies of this compilation. All namespaces inside referenced
    /// compilations will be visible to this one.
    /// </summary>
    public ImmutableArray<Compilation> ReferencedCompilations { get; }

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
                field = Binder.BindScripts(this);
            return field;
        }
    }

    public NamespaceSymbol RootNamespace
    {
        get
        {
            field ??= Binder.BindRootNamespace(this);
            return field;
        }
        private set;
    }

    public GlobalScope GlobalScope
    {
        get
        {
            field ??= new GlobalScope(this);
            return field;
        }
    }

    public BaseLibrary BaseLibrary
    {
        get
        {
            field ??= new(this);
            return field;
        }
    }


    private Compilation(ImmutableArray<SyntaxTree> syntaxTrees, ImmutableArray<Compilation> referencedCompilations)
    {
        SyntaxTrees = syntaxTrees;
        ReferencedCompilations = referencedCompilations;
    }

    public static Compilation From(ImmutableArray<SyntaxTree> syntaxTrees)
        => new([.. syntaxTrees], []);
    
    public static Compilation From(IEnumerable<SourceText> texts)
        => From([.. texts.Select(Parser.Parse)]);
    
    public static Compilation From(SourceText text)
        => From([text]);

    public Compilation AddReference(Compilation referencedCompilation)
        => new(SyntaxTrees, [referencedCompilation, .. ReferencedCompilations]);

    public Compilation WithoutReference(Compilation compilation)
        => new(SyntaxTrees, [.. ReferencedCompilations.Where(c => c != compilation)]);
    
    public Compilation AddBaseLibrary()
        => AddReference(LoadBaseLibrary());
    

    public static Compilation LoadBaseLibrary()
    {
        var fileNames = Directory.EnumerateFiles(BaseLibraryDirectory, "*.axl", SearchOption.AllDirectories);
        return From(fileNames.Select(SourceText.LoadFile));
    }
    

    private void Complete()
    {
        var sideEffects = new BinderSideEffects();
        
        BaseLibrary.Complete(sideEffects.DiagnosticBag);
        RootNamespace.Complete(sideEffects);
        foreach (var script in Scripts)
            script.Complete(sideEffects);

        Diagnostics = [.. SyntaxTrees.SelectMany(tree => tree.Diagnostics), ..sideEffects.DiagnosticBag.Drain()];
        SemanticSideTable = sideEffects.SemanticSideTable;
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