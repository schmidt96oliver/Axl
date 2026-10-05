using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public closed class NamespaceOrTypeSymbol(string name) : Symbol(name)
{
    public ImmutableArray<Symbol> Members
    {
        get
        {
            Guard.IsState(!field.IsDefault);
            return field;
        }
        internal set
        {
            // Set exactly once during construction.
            Guard.IsState(field.IsDefault);
            field = value;
        }
    }
    
    public Symbol? LookupMember(string name)
        => name is ""
            ? null
            : Members.SingleOrDefault(symbol => symbol.Name == name);
}