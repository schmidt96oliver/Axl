namespace Axl.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];
    
    public bool HasError { get; private set; }
    
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();


    public ErrorGuaranteed ReportError(Diagnostic.Error error)
    {
        _diagnostics.Add(error);
        return ErrorGuaranteed.Instance;
    }

    public void Report(Diagnostic.Lint lint)
        => _diagnostics.Add(lint);


}