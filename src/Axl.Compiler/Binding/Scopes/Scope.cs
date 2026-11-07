using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public closed class Scope
{
    public Compilation Compilation => Owner.Compilation;

    public Scope? EnclosingScope { get; }

    /// <summary>
    /// The innermost <see cref="Symbol"/> the binder is currently
    /// inside.
    /// </summary>
    public virtual Symbol Owner { get; }

    public BindingFlags Flags { get; }

    public ImmutableArray<Symbol> VisibleSymbols
    {
        get
        {
            if (field.IsDefault)
                field = MakeVisibleSymbols();
            return field;
        }
    }
    
    
    
    public FunSymbol? OwnerAsFun => Owner as FunSymbol;

    public TypeSymbol? OwnerAsType => Owner as TypeSymbol;

    public NamespaceOrTypeSymbol? OwnerAsNamespaceOrType => Owner as NamespaceOrTypeSymbol;

    public TypeSymbol? InstanceType => OwnerAsFun?.SelfParameter?.Type;

    public bool IsStatic => OwnerAsFun?.SelfParameter is null;

    public bool IsInsideLoop => (Flags & BindingFlags.InsideLoop) != 0;


    
    protected Scope(Scope enclosingScope, Symbol? owner = null, BindingFlags? flags = null)
        => (EnclosingScope, Owner, Flags)
            = (enclosingScope, owner ?? enclosingScope.Owner, flags ?? enclosingScope.Flags);

    protected Scope(Scope? enclosingScope, Symbol owner, BindingFlags flags)
        => (EnclosingScope, Owner, Flags) = (enclosingScope, owner, flags);


    public virtual ImmutableArray<Symbol> Lookup(string name)
        => [.. VisibleSymbols.Where(s => s.Name == name)];

    /// <summary>
    /// Called only once and then cached. Returns all symbols that are visible
    /// inside _this_ scope.
    /// </summary>
    public abstract ImmutableArray<Symbol> MakeVisibleSymbols();
    
    

    public bool IsAccessible(Symbol symbol)
        => symbol.IsAccessibleFrom(Owner);

    public Scope WithOwner(Symbol owner)
        => new OwnerScope(this, owner);
}