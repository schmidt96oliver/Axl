using System.Collections.Immutable;
using System.Diagnostics;
using AxlCompiler.Binding;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Diagnostics;
using AxlCompiler.Mir;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Text;
using Binder = AxlCompiler.Binding.Binder;

namespace AxlCompiler;

public sealed record LoweredFunBody(MutMirFunction MirFunction, ImmutableArray<Diagnostic> Diagnostics);

public class Compilation
{
    public static string BaseLibraryDirectory
        => Path.Combine(AppContext.BaseDirectory, "BaseLib");

    private readonly Dictionary<FunSymbol, LoweredFunBody> _loweredBodies = [];
    
    
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

    /// <summary>
    /// Enumerates all funs with a body contained in this compilation, excluding
    /// those in <see cref="ReferencedCompilations"/> and excluding intrinsics.
    /// </summary>
    public IEnumerable<FunDefinitionSymbol> AllFuns
    {
        get
        {
            return [.. Inner(RootNamespace), .. Scripts.SelectMany(Inner)];

            IEnumerable<FunDefinitionSymbol> Inner(Symbol symbol) => symbol switch
            {
                FunDefinitionSymbol funSymbol => [funSymbol],
                FunSymbol => throw new UnreachableException("Specialized funs should not be on compilation."),
                ScriptSymbol script => [.. Inner(script.Fun), .. script.Members.SelectMany(Inner)],
                
                _ => symbol.Members.SelectMany(Inner),
            };
        }
    }

    /// <summary>
    /// A token used for extracting information from the binder through its
    /// side effects. E.g. used by the SemanticModel to get visible symbols.
    /// </summary>
    public Token? Probe { get; init; }

    public SemanticModel SemanticModel
    {
        get
        {
            field ??= new SemanticModel(this);
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

    public Compilation With(Compilation referencedCompilation)
        => new(SyntaxTrees, [referencedCompilation, .. ReferencedCompilations]);

    public Compilation Without(Compilation referencedCompilation)
        => new(SyntaxTrees, [.. ReferencedCompilations.Where(c => c != referencedCompilation)]);

    public Compilation Without(SyntaxTree tree)
        => new([.. SyntaxTrees.Except([tree])], ReferencedCompilations);

    public Compilation With(SyntaxTree tree)
        => new([.. SyntaxTrees, tree], ReferencedCompilations);

    public Compilation WithProbe(Token probe)
    {
        var compilation = new Compilation(SyntaxTrees, ReferencedCompilations)
        {
            Probe = probe
        };
        return compilation;
    }

    public Compilation Replace(SyntaxTree tree, SyntaxTree with)
        => Without(tree).With(with);
    
    public Compilation WithBaseLibrary()
        => With(LoadBaseLibrary());

    
    

    public static Compilation LoadBaseLibrary()
    {
        var fileNames = Directory.EnumerateFiles(BaseLibraryDirectory, "*.axl", SearchOption.AllDirectories);
        return From(fileNames.Select(SourceText.LoadFile));
    }
    

    private void Complete()
    {
        var sideEffects = new BinderSideEffects();
        Complete(sideEffects);

        Diagnostics = sideEffects.DiagnosticBag.Drain();
        SemanticSideTable = sideEffects.SemanticSideTable;
    }

    public void Complete(BinderSideEffects sideEffects)
    {
        sideEffects.DiagnosticBag.AddRange(SyntaxTrees.SelectMany(tree => tree.Diagnostics));

        BaseLibrary.Complete(sideEffects.DiagnosticBag);
        RootNamespace.Complete(sideEffects);
        foreach (var script in Scripts)
            script.Complete(sideEffects);

        sideEffects.DiagnosticBag.AddRange(AllFuns
            .Where(f => f.Body is BoundBlock)
            .SelectMany(fun => GetLoweredFunBody(fun).Diagnostics));
    }

    public LoweredFunBody GetLoweredFunBody(FunDefinitionSymbol fun)
    {
        if (fun.Body is not BoundBlock)
            throw new ArgumentException("Cannot lower fun without a bound block.", nameof(fun));

        if (fun.Compilation != this)
            return fun.Compilation.GetLoweredFunBody(fun);

        if (_loweredBodies.TryGetValue(fun, out var loweredFun))
            return loweredFun;

        var mirFun = Lowerer.Lower(fun, this);
        var bag = new DiagnosticBag();
        mirFun.Analyze(fun, bag);

        var loweredBody = new LoweredFunBody(mirFun, bag.Drain());
        _loweredBodies[fun] = loweredBody;
        return loweredBody;
    }


    

    public CompilationResult Compile(ScriptSymbol entryScript)
    {
        if (entryScript.Compilation != this || !Scripts.Contains(entryScript))
            throw new ArgumentException("The given script is not part of this compilation.", nameof(entryScript));

        var globalSideEffects = new BinderSideEffects();

        foreach (var referencedCompilation in ReferencedCompilations)
            referencedCompilation.Complete(globalSideEffects);
        Complete(globalSideEffects);

        if (globalSideEffects.DiagnosticBag.HasError)
            return new CompilationResult(null, globalSideEffects.DiagnosticBag.Drain());
        
        var program = Monomorphizer.TryMonomorphize(entryScript.Fun,
            funDefinition => GetLoweredFunBody(funDefinition).MirFunction,
            globalSideEffects.DiagnosticBag);
        return new CompilationResult(program, globalSideEffects.DiagnosticBag.Drain());
    }
    

    public SyntaxTree SyntaxTreeFor(SourceText text)
    {
        return SyntaxTrees.FirstOrDefault(tree => tree.SourceText == text)
               ?? throw new ArgumentException($"Syntax tree for given {nameof(text)} does not exit.", nameof(text));
    }

    public SyntaxTree SyntaxTreeFor(SourceLocation location)
        => SyntaxTreeFor(location.SourceText);

}