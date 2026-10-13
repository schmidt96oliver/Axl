using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class BlockScope : Scope
{
    private readonly ImmutableArray<Symbol> _localMembers;
    private readonly ImmutableArray<VariableSymbol> _variables;

    private BlockScope(Scope enclosingScope, ImmutableArray<Symbol> localMembers,
        ImmutableArray<VariableSymbol> variables)
        : base(enclosingScope)
    {
        _localMembers = localMembers;
        _variables = variables;
    }


    /// <summary>
    /// Creates a new block scope without local funs or variables
    /// that inherits flags from <paramref name="enclosingScope"/>.
    /// </summary>
    public BlockScope(Scope enclosingScope, ImmutableArray<Symbol> localMembers)
        : this(enclosingScope, localMembers, [])
    {
    }

    public BlockScope WithVariable(VariableSymbol variable)
    {
        Guard.IsState(EnclosingScope is not null, "Block scopes must always have an enclosing scope.");

        // The new variable will shadow those with the same name.

        var adjacentVars = _variables.Where(v => v.Name != variable.Name);
        return new BlockScope(EnclosingScope, _localMembers, [.. adjacentVars, variable]);
    }

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => [.. _localMembers.Where(fun => fun.Name == name), .. _variables.Where(variable => variable.Name == name)];
}