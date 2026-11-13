using System.Collections.Immutable;
using System.Diagnostics;
using AxlCompiler.Binding;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

// Because root is created as SourceNamespaceSymbol as well, we need
// to pass compilation and owner separately and cannot derive it from
// scope. Otherwise, asking the GlobalScope for it starts an infinite loop :/.
public sealed class SourceNamespaceSymbol(
    Compilation compilation,
    NamespaceDeclaration declaration,
    Symbol? owner,
    Scope enclosingScope,
    bool isRoot)
    : NamespaceSymbol(compilation, declaration.Name, owner, isRoot)
{
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingMembers = false;

    private ImmutableArray<LazyUsingsScope> _scopes;
    
    public NamespaceDeclaration Declaration { get; } = declaration;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [.. Declaration.SyntaxTrees.Select(tree => tree.FileSyntax)];
    
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

            // Global scope already sees root namespace.
            var binder = new Binder(Owner is null 
                    ? enclosingScope.WithOwner(this) 
                    : new MemberScope(enclosingScope, this),
                _sideEffects);
            
            // Binding namespace files produces the usings scopes of
            // that file, which is lazy and must be completed by this
            // symbol.
            var scopes = ImmutableArray.CreateBuilder<LazyUsingsScope>();
            
            field =
            [
                .. Declaration.MemberRoots.SelectMany(memberRoot => BindNamespaceFile(memberRoot)),
                .. Declaration.Children.Select(binder.BindNamespaceDeclaration)
            ];

            _scopes = scopes.DrainToImmutable();

            _isBindingMembers = false;

            // The following checks accesses members, so they need to run
            // after binding is done.
            binder.CheckDuplicateDeclarations(field);

            return field;

            ImmutableArray<Symbol> BindNamespaceFile(FileSyntax memberRoot)
            {
                var result = binder.BindNamespaceFile(memberRoot, out var scope);
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
        
        sideEffects.MergeWith(_sideEffects);
    }
}