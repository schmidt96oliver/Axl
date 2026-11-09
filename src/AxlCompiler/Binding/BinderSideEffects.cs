using System.Collections.Immutable;
using AxlCompiler.Diagnostics;
using AxlCompiler.Symbols;
using AxlCompiler.Text;

namespace AxlCompiler.Binding;

public sealed class BinderSideEffects
{
    public DiagnosticBag DiagnosticBag { get; } = new();
    public SemanticSideTable SemanticSideTable { get; } = new();

    public ImmutableArray<Symbol>? VisibleSymbolsAtProbe { get; private set; }


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
        foreach (var (location, symbol) in sideEffects.SemanticSideTable.ResolvedSymbols)
            AddResolvedSymbol(location, symbol);

        VisibleSymbolsAtProbe ??= sideEffects.VisibleSymbolsAtProbe;
    }

    public void AddVisibleSymbols(ImmutableArray<Symbol> symbols)
    {
        // Ignore if the probe has already been answered. This will not
        // commonly happen anyway. Only if the user wrote the probe itself :D.
        if (VisibleSymbolsAtProbe is not null)
            return;
        VisibleSymbolsAtProbe = symbols;
    }
    
}