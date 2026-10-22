using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax;

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

    private FrozenDictionary<SyntaxTree, UsingsScope>? _fileScopes;
    
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
                _fileScopes ??= Declaration.Syntaxes
                    .ToFrozenDictionary(tree => tree, tree => _binder.BindFileScope(tree.FileSyntax));
                
                field =
                [
                    .. Declaration.Syntaxes.SelectMany(tree => _binder.WithScope(_fileScopes[tree]).BindNamespaceFile(tree)),
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
        
        Debug.Assert(_fileScopes is not null);
        foreach (var scope in _fileScopes.Values)
            scope.Complete(sideEffects);
        
        _binder!.SideEffects.DrainInto(sideEffects);
        _binder = null;
    }

    private Binder MakeBinder()
        // Global scope already sees root namespace.
        => new(Owner is null ? enclosingScope.WithOwner(this) : new MemberScope(enclosingScope, this),
            new BinderSideEffects());
}