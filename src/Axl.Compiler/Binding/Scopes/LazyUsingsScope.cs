using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Binding.Scopes;

public sealed class LazyUsingsScope(Scope enclosingScope, ImmutableArray<UsingDirectiveSyntax> usingSyntaxes)
    : Scope(enclosingScope)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _binder = null;
    private bool _isBindingUsings = false;

    private ImmutableArray<NamespaceSymbol> Usings
    {
        get
        {
            if (field.IsDefault)
            {
                if (_isBindingUsings)
                    throw new CycleException();
                _isBindingUsings = true;

                _binder ??= new Binder(EnclosingScope!, new BinderSideEffects());
                field = [.. usingSyntaxes.Select(_binder.BindUsingDirective).Where(ns => ns is not null)!];
                
                _isBindingUsings = false;
            }

            return field;
        }
    }
    
    
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
    {
        return [
        .. Usings.SelectMany(u => u.LookupMembers(name))];
    }

    public void Complete(BinderSideEffects sideEffects)
    {
        _ = Usings;
        _binder!.SideEffects.DrainInto(sideEffects);
    }
}