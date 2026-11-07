using System.Collections.Frozen;
using System.Collections.Immutable;
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

    public string DisplayName { get; }

    public ImmutableArray<Place.Local> ParameterPlaces { get; }

    public Place.Local ReturnPlace { get; }


    public IReadOnlyList<Place.Local> Locals => _locals.AsReadOnly();

    public IReadOnlyList<MutBasicBlock> BasicBlocks => _basicBlocks.AsReadOnly();
    
    public MutBasicBlock Entry => BasicBlocks[0];


    public MutMirFunction(string displayName, Place.Local returnPlace, ImmutableArray<Place.Local> parameterPlaces)
    {
        DisplayName = displayName;

        Guard.MustBe(returnPlace.Id.Value == 0, $"{nameof(returnPlace)} must be first local.");
        Guard.MustBe(returnPlace.Kind is LocalKind.Return);
        Guard.MustBe(Enumerable.Range(0, parameterPlaces.Length).All(i => parameterPlaces[i].Id.Value == i + 1),
            $"{nameof(parameterPlaces)} must follow each other directly after {nameof(returnPlace)}");
        Guard.MustBe(parameterPlaces.All(p => p.Kind is LocalKind.Parameter));
        
        ReturnPlace = returnPlace;
        ParameterPlaces = parameterPlaces;
        _locals = [ReturnPlace, .. ParameterPlaces];
        _nextLocal = ParameterPlaces.Length > 0 ? ParameterPlaces[^1].Id.Value + 1 : 1;
        
        AddBasicBlock();
    }

    /// <summary>
    /// Runs all checks that produce diagnostics.
    /// </summary>
    public void Analyze(FunDefinitionSymbol funDefinition, DiagnosticBag diagnosticBag)
    {
        ReturnCheck.Check(funDefinition, this, diagnosticBag);
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

    public void AddBasicBlock(MutBasicBlock block)
    {
        Guard.MustBe(block.MirFunction == this);
        Guard.MustBe(_basicBlocks.All(b => b.Id != block.Id));
        _basicBlocks.Add(block);
        _nextBasicBlock = _basicBlocks.Max(b => b.Id.Value) + 1;
    }

    public int RemoveAllBasicBlocks(Predicate<MutBasicBlock> predicate)
    {
        var removed = _basicBlocks.RemoveAll(predicate);
        if (removed > 0)
            CfgVersion++;
        return removed;
    }

    public MutBasicBlock GetBasicBlock(BasicBlockId id)
    {
        return _basicBlocks.First(b => b.Id == id);
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

    public void AddLocal(Place.Local local)
    {
        Guard.MustBe(_locals.All(l => l.Id != local.Id));
        _locals.Add(local);
        _nextLocal = _locals.Max(l => l.Id.Value) + 1;
    }
    
    public Place.Local AddTemp(TypeSymbol type)
        => AddLocal(type, LocalKind.Temp);
}