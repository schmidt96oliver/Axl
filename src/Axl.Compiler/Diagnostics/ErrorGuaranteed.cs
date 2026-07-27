namespace Axl.Compiler.Diagnostics;

/// <summary>
/// A token, only returned by <see cref="DiagnosticBag"/> when reporting an error.
/// That way, error nodes can ask for prove, that an error has actually been reported.
/// So no error node can ever be constructed without reporting an error.
/// </summary>
public record ErrorGuaranteed
{
    /// <summary>
    /// NEVER access this. Only <see cref="DiagnosticBag"/> may return this token.
    /// </summary>
    internal static readonly ErrorGuaranteed Instance = new();

    private ErrorGuaranteed()
    {
        
    }
}