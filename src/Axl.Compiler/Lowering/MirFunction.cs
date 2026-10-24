using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Lowering;

public sealed class MirFunction
{
    private int _nextLocal = 0;
    private int _nextBasicBlock = 0;

    public FunSymbol FunSymbol { get; }
    
    public List<Place.Local> Locals { get; } = [];
    public List<Place.Local> Parameters { get; } = [];

    public Place.Local? ReceiverPlace { get; }

    public Place.Local ReturnPlace { get; }

    public List<BasicBlock> BasicBlocks { get; } = [];
    
    public BasicBlock Entry => BasicBlocks[0];


    public MirFunction(FunSymbol funSymbol)
    {
        FunSymbol = funSymbol;
        ReturnPlace = NewLocal(funSymbol.ReturnType);
        ReceiverPlace = funSymbol.ReceiverType is not null ? NewLocal(funSymbol.ReceiverType) : null;
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

    public Place.Local NewLocal(TypeSymbol type, SyntaxNode? syntax = null)
    {
        var id = new LocalId(_nextLocal);
        _nextLocal++;
        var local = new Place.Local(id, type, syntax);
        Locals.Add(local);
        return local;
    }
}