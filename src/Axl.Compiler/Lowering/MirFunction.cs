using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Lowering;

public sealed class MirFunction
{
    private int _nextLocal = 0;
    private int _nextBasicBlock = 0;

    private readonly FrozenDictionary<ParameterSymbol, Place.Local> _parameterPlaces;
    
    public FunSymbol FunSymbol { get; }
    
    public List<Place.Local> Locals { get; } = [];

    public ImmutableArray<ParameterSymbol> Parameters { get; }

    public List<BasicBlock> BasicBlocks { get; } = [];
    
    public BasicBlock Entry => BasicBlocks[0];


    public MirFunction(FunSymbol funSymbol)
    {
        FunSymbol = funSymbol;

        Parameters = FunSymbol.SelfParameter is not null
            ? [FunSymbol.SelfParameter, .. FunSymbol.Parameters]
            : FunSymbol.Parameters;

        _parameterPlaces = Enumerable.ToDictionary(Parameters,
                param => param, param => NewLocal(IsParameterByRef(param)
                    ? new RefTypeSymbol(param.Type)
                    : param.Type, param.DeclarationSyntax))
            .ToFrozenDictionary();
        
        NewBasicBlock();
    }

    public Place.Local GetParameterPlace(ParameterSymbol parameter)
        => _parameterPlaces[parameter];

    public static bool IsParameterByRef(ParameterSymbol parameter)
        => parameter.IsMut;
    

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