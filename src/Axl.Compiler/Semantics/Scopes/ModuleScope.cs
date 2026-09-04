using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Scopes;

public sealed class ModuleScope(ModuleSymbol moduleSymbol, Scope? parent) 
    : Scope(parent)
{
    protected override ImmutableArray<Symbol> LookupOnThisScope(SymbolName name)
        => [.. moduleSymbol.Members.Where(member => member.Name == name)];
}