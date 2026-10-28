namespace Axl.Compiler.Mir;

public readonly record struct BasicBlockId(int Value)
{
    /// <inheritdoc />
    public override string ToString()
        => $"bb{Value}";
}

public sealed class MutBasicBlock(BasicBlockId id, MutMirFunction mirFunction, string debugLabel = "")
{
    private readonly List<Instruction> _instructions = [];
    
    public BasicBlockId Id { get; } = id;
    public MutMirFunction MirFunction { get; } = mirFunction;
    public string DebugLabel { get; } = debugLabel;
    public IReadOnlyList<Instruction> Instructions => _instructions.AsReadOnly();


    public Terminator? Terminator
    {
        get;
        set
        {
            field = value;
            MirFunction.CfgVersion++;
        }
    }

    public void Add(Instruction instruction)
    {
        _instructions.Add(instruction);
        
        // Adding an instruction does not change the CFG.
        MirFunction.Version++;
    }
}