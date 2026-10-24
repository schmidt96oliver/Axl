namespace Axl.Compiler.Lowering;

public readonly record struct BasicBlockId(int Value)
{
    /// <inheritdoc />
    public override string ToString()
        => $"bb{Value}";
}

public sealed class BasicBlock(BasicBlockId id, string debugLabel = "")
{
    public string DebugLabel { get; } = debugLabel;
    public BasicBlockId Id { get; } = id;
    public List<Instruction> Instructions { get; } = [];
    public Terminator? Terminator { get; set; }
}