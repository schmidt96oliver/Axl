using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class ScriptScope(GlobalScope globalScope) 
    : Scope(globalScope, owner: null!, BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => globalScope.Compilation.RootNamespace.LookupMembers(name);
}