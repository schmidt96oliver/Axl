using AxlCompiler.Binding;

namespace AxlCompiler.Mir.Interpreter;

public closed record MiriResult
{
    public sealed record Ok(ConstValue Value) : MiriResult;

    public sealed record Panicked : MiriResult;

    public sealed record OutOfFuel : MiriResult;

    public sealed record CannotEvaluateConst : MiriResult;
}