namespace Axl.Compiler.Testing;

public readonly record struct Evaluation(bool HasFailed, bool HasUnsupportedFeatures, string Message)
{
    public static readonly Evaluation Succeeded = new(false, false, string.Empty);
    
    public static Evaluation Failed(string message)
        => new(true, false, message);
    public static Evaluation Unsupported(string message)
        => new(false, true, message);
}