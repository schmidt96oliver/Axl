using System.Collections.Immutable;

namespace Axl.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
    private bool _isDrained = false;
    
    public bool HasError { get; private set; }
    
    
    public ImmutableArray<Diagnostic> Drain()
    {
        Guard.IsState(!_isDrained);
        
        _isDrained = true;
        return _diagnostics.DrainToImmutable();
    }
    
    public void DrainInto(DiagnosticBag bag)
    {
        Guard.IsState(!_isDrained);
        
        _isDrained = true;
        
        bag.AddRange(_diagnostics);
        _diagnostics.Clear();
    }
    

    public void ReportError(Diagnostic.Error error)
    {
        Guard.IsState(!_isDrained);

        _diagnostics.Add(error);
        HasError = true;
    }

    public void ReportLint(Diagnostic.Lint lint)
    {
        Guard.IsState(!_isDrained);
        
        _diagnostics.Add(lint);
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
     => _diagnostics.AddRange(diagnostics);
    
    public void AddRange(ImmutableArray<Diagnostic> diagnostics)
     => _diagnostics.AddRange(diagnostics);

    
    
}