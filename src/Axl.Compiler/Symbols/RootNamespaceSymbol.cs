using System.Collections.Immutable;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;

namespace Axl.Compiler.Symbols;

public class RootNamespaceSymbol : NamespaceSymbol
{
    public BaseNamespaceSymbol BaseNamespace { get; }

    public NamespaceDeclaration Declaration { get; }
    
    
    public RootNamespaceSymbol(NamespaceDeclaration declaration) 
        : base(name: declaration.Name, owner: null)
    {
        BaseNamespace = new BaseNamespaceSymbol(owner: this);
        Declaration = declaration;
    }

    /// <summary>
    /// Namespaces are always public.
    /// </summary>
    public override bool IsPublic => true;

    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (field.IsDefault)
            {
                var scope = new RootScope(this);
                field =
                [
                    BaseNamespace,
                    .. Declaration.Children.Select(decl => new SourceNamespaceSymbol(decl, scope))
                ];
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);
    }
}