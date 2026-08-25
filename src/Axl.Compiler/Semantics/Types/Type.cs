namespace Axl.Compiler.Semantics.Types;

public abstract record AxlType;

public sealed record I32Type : AxlType;
public sealed record I64Type : AxlType;
public sealed record F32Type : AxlType;
public sealed record F64Type : AxlType;
public sealed record BoolType : AxlType;
public sealed record StringType : AxlType;
public sealed record NoneType : AxlType;

public sealed record NeverType : AxlType;
public sealed record ErrorType : AxlType;