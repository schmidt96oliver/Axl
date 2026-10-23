using System.Collections.Immutable;
using Axl.Compiler.Binding;

namespace Axl.Compiler.Symbols;

public class BaseLibRootSymbol : NamespaceSymbol
{
    private readonly BaseNamespaceSymbol _baseNamespace;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    public BaseLibRootSymbol(Compilation compilation) 
        : base(name: "", owner: null, isRoot: true)
    {
        _baseNamespace = new BaseNamespaceSymbol(owner: this);
    }

    public override ImmutableArray<Symbol> Members => [_baseNamespace];

    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);
    }
}