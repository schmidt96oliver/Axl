namespace Axl.Compiler.Semantics.Types;

public sealed class TypeContext()
{
    public I32Type I32 { get; } = new();
    public I64Type I64 { get; } = new();
    public F32Type F32 { get; } = new();
    public F64Type F64 { get; } = new();
    public BoolType Bool { get; } = new();
    public StringType String { get; } = new();
    
    public NoneType None { get; } = new();
    public NeverType Never { get; } = new();
    public ErrorType Error { get; } = new();
}