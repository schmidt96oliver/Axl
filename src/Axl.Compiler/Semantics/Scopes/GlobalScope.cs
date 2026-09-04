using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Scopes;

public sealed class GlobalScope(GlobalSymbol globalSymbol) 
    : Scope(parent: null)
{
    protected override ImmutableArray<Symbol> LookupOnThisScope(SymbolName name)
        => [.. globalSymbol.Members.Where(member => member.Name == name)];
}