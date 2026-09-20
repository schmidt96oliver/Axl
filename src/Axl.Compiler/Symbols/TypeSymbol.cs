using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class TypeSymbol : ModuleOrTypeSymbol
{
    private readonly Func<ImmutableArray<Symbol>> _memberFactory;
    public override SymbolKind Kind => SymbolKind.Type;


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
        : base(SymbolName.From(name))
    {
        _memberFactory = memberFactory;
    }
}