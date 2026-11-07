using System.Collections.Immutable;

namespace Axl.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    public static readonly DiagnosticBag Discard = new(isDiscard: true);
    
    private readonly ImmutableArray<Diagnostic>.Builder? _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
    private bool _isDrained = false;

    public bool HasError => _diagnostics?.OfType<Diagnostic.Error>().Any() ?? false;
    public IEnumerable<Diagnostic> Diagnostics => _diagnostics ?? Enumerable.Empty<Diagnostic>();

    public DiagnosticBag() 
        : this(isDiscard: false)
    {
        
    }

    private DiagnosticBag(bool isDiscard)
    {
        if (!isDiscard)
            _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
    }
    
    
    public ImmutableArray<Diagnostic> Drain()
    {
        Guard.IsState(!_isDrained);
        
        _isDrained = true;
        return _diagnostics?.DrainToImmutable() ?? [];
    }
    
    public void ReportError(Diagnostic.Error error)
    {
        Guard.IsState(!_isDrained);

        _diagnostics?.Add(error);
    }

    public void ReportLint(Diagnostic.Lint lint)
    {
        Guard.IsState(!_isDrained);
        
        _diagnostics?.Add(lint);
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
     => _diagnostics?.AddRange(diagnostics);
    
    public void AddRange(ImmutableArray<Diagnostic> diagnostics)
     => _diagnostics?.AddRange(diagnostics);

    
    
}