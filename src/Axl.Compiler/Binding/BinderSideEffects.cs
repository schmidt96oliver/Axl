using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Text;

namespace Axl.Compiler.Binding;

public sealed class BinderSideEffects
{
    public DiagnosticBag DiagnosticBag { get; } = new();
    public SemanticSideTable SemanticSideTable { get; } = new();

    public void ReportError(Diagnostic.Error? error)
    {
        if (error is not null)
            DiagnosticBag.ReportError(error);
    }

    public void ReportLint(Diagnostic.Lint? lint)
    {
        if (lint is not null)
            DiagnosticBag.ReportLint(lint);
    }
    
    public void AddResolvedSymbol(SourceLocation location, Symbol? symbol)
    {
        SemanticSideTable.AddResolvedSymbol(location, symbol);
    }

    /// <summary>
    /// Merges all given side effects into this object.
    /// </summary>
    public void AddFrom(BinderSideEffects sideEffects)
    {
        DiagnosticBag.AddRange(sideEffects.DiagnosticBag.Diagnostics);
        foreach (var (location, symbol) in SemanticSideTable.ResolvedSymbols)
            sideEffects.SemanticSideTable.AddResolvedSymbol(location, symbol);
    }
    
}