using System.Collections.Frozen;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Lowering;

public sealed class MirFunction
{
    private int _nextLocal = 0;
    private int _nextBasicBlock = 0;

    public FrozenDictionary<ParameterSymbol, Place.Local> ParameterPlaces { get; set; }

    public Place.Local ReturnPlace { get; set; }

    public string DebugName { get; }

    public List<Place.Local> Locals { get; } = [];

    public List<BasicBlock> BasicBlocks { get; } = [];
    
    public BasicBlock Entry => BasicBlocks[0];


    public MirFunction(string debugName)
    {
        DebugName = debugName;
        NewBasicBlock();
    }

    public BasicBlock NewBasicBlock(string debugLabel = "")
    {
        var id = new BasicBlockId(_nextBasicBlock);
        _nextBasicBlock++;
        var block = new BasicBlock(id, debugLabel);
        BasicBlocks.Add(block);
        return block;
    }

    public Place.Local NewLocal(TypeSymbol type, LocalKind kind, SyntaxNode? syntax = null)
    {
        var id = new LocalId(_nextLocal);
        _nextLocal++;
        var local = new Place.Local(id, kind, type, syntax);
        Locals.Add(local);
        return local;
    }
    
    public Place.Local NewTempLocal(TypeSymbol type)
        => NewLocal(type, LocalKind.Temp);
}