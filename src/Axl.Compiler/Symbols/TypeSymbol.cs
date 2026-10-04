using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public class TypeSymbol : NamespaceOrTypeSymbol
{
    private readonly Func<ImmutableArray<Symbol>> _memberFactory;


    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (field.IsDefault)
                field = _memberFactory();
            return field;
        }
    }

    public TypeSymbol(string name, Func<ImmutableArray<Symbol>> memberFactory) 
        : base(name)
    {
        _memberFactory = memberFactory;
    }
}