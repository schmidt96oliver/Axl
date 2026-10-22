using System.Collections.Immutable;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;

namespace Axl.Compiler.Symbols;

public sealed class SourceNamespaceSymbol(NamespaceDeclaration declaration, Scope enclosingScope)
    : NamespaceSymbol(declaration.Name, enclosingScope.Owner)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _binder = null;
    private bool _isBindingMembers = false;

    public NamespaceDeclaration Declaration { get; } = declaration;
    
    
    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (field.IsDefault)
            {
                if (_isBindingMembers)
                    throw new CycleException();
                _isBindingMembers = true;

                _binder ??= MakeBinder();
                field =
                [
                    .. Declaration.Syntaxes.SelectMany(tree => _binder.BindNamespaceFile(tree)),
                    .. Declaration.Children.Select(_binder.BindNamespaceDeclaration)
                ];

                _isBindingMembers = false;

                // The following checks accesses members, so they need to run
                // after binding is done.
                _binder.CheckDuplicateDeclarations(field);
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);

        _binder!.SideEffects.DrainInto(sideEffects);
        _binder = null;
    }

    private Binder MakeBinder()
        => new(new MemberScope(enclosingScope, this), new BinderSideEffects());

    
}