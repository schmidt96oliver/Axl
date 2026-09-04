namespace Axl.Compiler.Semantics.Types;

public sealed class TypeContext
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


    /// <summary>
    /// Whether a value of type <paramref name="source"/> can be
    /// assigned to a target of type <paramref name="target"/>.
    /// </summary>
    public bool IsAssignableTo(AxlType source, AxlType target)
        => (source, target) switch
        {
            // Errors are silent
            (ErrorType, _) or (_, ErrorType) => true,

            // Never assigns to anything
            (NeverType, _) => true,

            // All other combination are exact match only
            _ => source == target
        };
}