namespace Axl.Compiler.Semantics.Types;

public abstract record AxlType
{
    public abstract string DisplayName { get; }
}

public sealed record I32Type : AxlType
{
    public override string DisplayName => "i32";
}

public sealed record I64Type : AxlType
{
    public override string DisplayName => "i64";
}
public sealed record F32Type : AxlType
{
    public override string DisplayName => "f32";
}
public sealed record F64Type : AxlType
{
    public override string DisplayName => "f64";
}
public sealed record BoolType : AxlType
{
    public override string DisplayName => "bool";
}
public sealed record StringType : AxlType
{
    public override string DisplayName => "string";
}
public sealed record NoneType : AxlType
{
    public override string DisplayName => "none";
}

public sealed record NeverType : AxlType
{
    public override string DisplayName => "never";
}

public sealed record ErrorType : AxlType
{
    public override string DisplayName => "???";
}