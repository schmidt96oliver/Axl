using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Scopes;

public sealed class FnBodyScope(FnSymbol fnSymbol, Scope? parent) : Scope(parent)
{
    protected override ImmutableArray<Symbol> LookupOnThisScope(SymbolName name)
        => [.. fnSymbol.GetParameters().Where(param => param.Name == name)];
}