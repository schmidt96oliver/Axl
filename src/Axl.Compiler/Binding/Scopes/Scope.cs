using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public closed class Scope
{
    public Scope? EnclosingScope { get; }

    public BaseNamespaceSymbol BaseNamespace { get; }

    public Symbol Owner { get; }

    public FunSymbol? OwnerAsFun => Owner as FunSymbol;

    public TypeSymbol? OwnerAsType => Owner as TypeSymbol;

    public BindingFlags Flags { get; }

    public TypeSymbol? InstanceType
    {
        get
        {
            Debug.Assert(OwnerAsFun?.ReceiverType is null ||
                         OwnerAsFun.ReceiverType == OwnerAsFun.OwningType,
                "A funs receiver should always be its owning type. Extensions differ, but are not implemented yet.");
            return OwnerAsFun?.ReceiverType;
        }
    }

    public bool IsStatic => OwnerAsFun?.ReceiverType == null;

    public bool IsInsideLoop => (Flags & BindingFlags.InsideLoop) != 0;


    
    protected Scope(Scope enclosingScope, Symbol? owner = null, BindingFlags? flags = null,
        BaseNamespaceSymbol? baseNamespace = null)
        => (EnclosingScope, Owner, Flags, BaseNamespace)
            = (enclosingScope, owner ?? enclosingScope.Owner, flags ?? enclosingScope.Flags,
                baseNamespace ?? enclosingScope.BaseNamespace);

    protected Scope(Scope? enclosingScope, Symbol owner, BindingFlags flags, BaseNamespaceSymbol baseNamespace)
        => (EnclosingScope, Owner, Flags, BaseNamespace)
            = (enclosingScope, owner, flags, baseNamespace);
    

    public abstract ImmutableArray<Symbol> Lookup(string name);

    public bool IsAccessible(Symbol symbol)
        => Owner.SelfAndOwners().Contains(symbol.AccessibleWithin);
}