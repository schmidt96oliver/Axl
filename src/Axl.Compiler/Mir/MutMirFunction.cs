using System.Collections.Frozen;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Mir.Analysis;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Mir;

public sealed class MutMirFunction
{
    private int _nextLocal = 0;
    private int _nextBasicBlock = 0;

    private readonly List<Place.Local> _locals = [];
    private readonly List<MutBasicBlock> _basicBlocks = [];

    /// <summary>
    /// Incremented each time the control flow graph changes. Same <see cref="CfgVersion"/>
    /// numbers mean, that the control flow graph is unchanged.
    /// </summary>
    public int CfgVersion
    {
        get;
        internal set
        {
            field = value;
            Version++;
        }
    } = 0;

    /// <summary>
    /// Incremented each time, the control flow graph or block instructions change.
    /// Same <see cref="Version"/> numbers mean, that the entire mir is unchanged.
    /// </summary>
    public int Version { get; internal set; } = 0;

    public string DebugName => Symbol.QualifiedName;
    
    public FunSymbol Symbol { get; }

    public Compilation Compilation => Symbol.Compilation;

    public FrozenDictionary<ParameterSymbol, Place.Local> ParameterPlaces { get; }

    public Place.Local ReturnPlace { get; }


    public IReadOnlyList<Place.Local> Locals => _locals.AsReadOnly();

    public IReadOnlyList<MutBasicBlock> BasicBlocks => _basicBlocks.AsReadOnly();
    
    public MutBasicBlock Entry => BasicBlocks[0];


    public MutMirFunction(FunSymbol fun, TypeSymbol returnType, Func<ParameterSymbol, TypeSymbol> getParameterType)
    {
        Symbol = fun;

        ReturnPlace = AddLocal(returnType, LocalKind.Return);
        ParameterPlaces = Enumerable
            .ToDictionary(fun.SelfAndParameters,
                param => param,
                param => AddLocal(getParameterType(param), LocalKind.Parameter, param.DeclarationSyntax))
            .ToFrozenDictionary();
        
        AddBasicBlock();
    }

    /// <summary>
    /// Runs all checks that produce diagnostics.
    /// </summary>
    public void Analyze(DiagnosticBag diagnosticBag)
    {
        var reachability = Reachability.Analyze(this);
        ReturnCheck.Check(this, reachability, diagnosticBag);
    }
    

    public MutBasicBlock AddBasicBlock(string debugLabel = "")
    {
        var id = new BasicBlockId(_nextBasicBlock);
        _nextBasicBlock++;
        var block = new MutBasicBlock(id, this, debugLabel);
        
        CfgVersion++;
        _basicBlocks.Add(block);

        return block;
    }

    public Place.Local AddLocal(TypeSymbol type, LocalKind kind, SyntaxNode? syntax = null)
    {
        var id = new LocalId(_nextLocal);
        _nextLocal++;
        var local = new Place.Local(id, kind, type, syntax);

        Version++;
        _locals.Add(local);
        
        return local;
    }
    
    public Place.Local AddTemp(TypeSymbol type)
        => AddLocal(type, LocalKind.Temp);
}