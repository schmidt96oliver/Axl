using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Text;

namespace Axl.Compiler.Binding;

public sealed class BinderSideEffects
{
    private bool _isDrained = false;
    
    public DiagnosticBag DiagnosticBag { get; } = new();
    public SemanticSideTable SemanticSideTable { get; } = new();

    public void ReportError(Diagnostic.Error error)
    {
        Guard.IsState(!_isDrained);
        DiagnosticBag.ReportError(error);
    }
    
    public void AddResolvedSymbol(SourceLocation location, Symbol? symbol)
    {
        Guard.IsState(!_isDrained);
        SemanticSideTable.AddResolvedSymbol(location, symbol);
    }

    /// <summary>
    /// Adds all side effects inside this object into <paramref name="sideEffects"/>.
    /// This object is not usable thereafter.
    /// </summary>
    /// <param name="sideEffects"></param>
    public void DrainInto(BinderSideEffects sideEffects)
    {
        Guard.IsState(!_isDrained);
        _isDrained = true;
        
        DiagnosticBag.DrainInto(sideEffects.DiagnosticBag);
        foreach (var (location, symbol) in SemanticSideTable.ResolvedSymbols)
            sideEffects.SemanticSideTable.AddResolvedSymbol(location, symbol);
    }
}