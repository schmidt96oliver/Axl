using System.Collections.Immutable;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Diagnostics;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Binding;

public sealed class BinderSideEffects
{
    public DiagnosticBag DiagnosticBag { get; } = new();

    public ImmutableArray<Symbol>? VisibleSymbolsAtProbe { get; private set; }
    public Scope? ScopeAtProbe { get; private set; }

    public Dictionary<ExprSyntax, List<BoundNode>> BoundNodes { get; } = [];


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
    
    /// <summary>
    /// Merges all given side effects into this object.
    /// </summary>
    public void MergeWith(BinderSideEffects sideEffects)
    {
        DiagnosticBag.AddRange(sideEffects.DiagnosticBag.Diagnostics);
        VisibleSymbolsAtProbe ??= sideEffects.VisibleSymbolsAtProbe;
        ScopeAtProbe ??= sideEffects.ScopeAtProbe;

        foreach (var kvp in sideEffects.BoundNodes)
        {
            if (BoundNodes.TryGetValue(kvp.Key, out var boundNodes))
                boundNodes.AddRange(kvp.Value);
            else
                BoundNodes[kvp.Key] = [.. kvp.Value];
        }
    }

    public void ResolveProbe(ImmutableArray<Symbol> symbols, Scope scope)
    {
        // Ignore if the probe has already been answered. This will not
        // commonly happen anyway. Only if the user wrote the probe itself :D.
        if (VisibleSymbolsAtProbe is not null || ScopeAtProbe is not null)
            return;
        VisibleSymbolsAtProbe = symbols;
        ScopeAtProbe = scope;
    }

    public void ResolveBoundNode(ExprSyntax syntax, BoundNode node)
    {
        //Debug.Assert(node.Syntax == syntax);
        //TODO: What to do if syntax and bound node diverge??
        if (BoundNodes.TryGetValue(syntax, out var boundNodes))
            boundNodes.Add(node);
        else
            BoundNodes[syntax] = [node];
    }
}