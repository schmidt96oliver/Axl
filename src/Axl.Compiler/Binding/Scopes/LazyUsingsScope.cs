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

    private ImmutableArray<NamespaceSymbol> Usings
    {
        get
        {
            if (!field.IsDefault)
                return field;

            if (_isBindingUsings)
                throw new CycleException();
            _isBindingUsings = true;

            field = new Binder(EnclosingScope!, _sideEffects).BindUsingDirectives(usingSyntaxes);
            _isBindingUsings = false;

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
        sideEffects.AddFrom(_sideEffects);
    }
}