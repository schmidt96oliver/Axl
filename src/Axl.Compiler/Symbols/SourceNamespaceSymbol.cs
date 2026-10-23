using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public sealed class SourceNamespaceSymbol(NamespaceDeclaration declaration, Scope enclosingScope, bool isRoot)
    : NamespaceSymbol(declaration.Name, enclosingScope.Owner, isRoot)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _binder = null;
    private bool _isBindingMembers = false;

    private ImmutableArray<LazyUsingsScope> _scopes;
    
    public NamespaceDeclaration Declaration { get; } = declaration;

    /// <summary>
    /// A source declared namespace is nameable, only if
    /// it is not the root.
    /// </summary>
    public override bool CanBeReferencedByName => !IsRoot;
    

    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (!field.IsDefault) return field;

            if (_isBindingMembers)
                throw new CycleException();
            _isBindingMembers = true;

            _binder ??= MakeBinder();
            
            // Binding namespace files produces the usings scopes of
            // that file, which is lazy and must be completed by this
            // symbol.
            var scopes = ImmutableArray.CreateBuilder<LazyUsingsScope>();
            
            field =
            [
                .. Declaration.SyntaxTrees.SelectMany(tree => BindNamespaceFile(tree)),
                .. Declaration.Children.Select(_binder.BindNamespaceDeclaration)
            ];

            _scopes = scopes.DrainToImmutable();

            _isBindingMembers = false;

            // The following checks accesses members, so they need to run
            // after binding is done.
            _binder.CheckDuplicateDeclarations(field);

            return field;

            ImmutableArray<Symbol> BindNamespaceFile(SyntaxTree tree)
            {
                var result = _binder.BindNamespaceFile(tree, out var scope);
                scopes.Add(scope);
                return result;
            }
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);
        
        Debug.Assert(!_scopes.IsDefault);
        foreach (var scope in _scopes)
            scope.Complete(sideEffects);
        
        _binder!.SideEffects.DrainInto(sideEffects);
        _binder = null;
    }

    private Binder MakeBinder()
        // Global scope already sees root namespace.
        => new(Owner is null ? enclosingScope.WithOwner(this) : new MemberScope(enclosingScope, this),
            new BinderSideEffects());
}