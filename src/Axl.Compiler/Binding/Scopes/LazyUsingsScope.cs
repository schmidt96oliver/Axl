using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Binding.Scopes;

public sealed class LazyUsingsScope(Scope enclosingScope, ImmutableArray<UsingDirectiveSyntax> usingSyntaxes)
    : Scope(enclosingScope)
{
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingUsings = false;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
    {
        if (_isBindingUsings)
            throw new CycleException();
        _isBindingUsings = true;

        var usedNamespaces = new Binder(EnclosingScope!, _sideEffects).BindUsingDirectives(usingSyntaxes);
        _isBindingUsings = false;

        return [.. usedNamespaces.SelectMany(ns => ns.Members)];
    }

    public void Complete(BinderSideEffects sideEffects)
    {
        _ = VisibleSymbols;
        sideEffects.AddFrom(_sideEffects);
    }
}