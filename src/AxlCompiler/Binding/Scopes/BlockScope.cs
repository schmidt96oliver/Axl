using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding.Scopes;

public sealed class BlockScope : Scope
{
    private readonly ImmutableArray<Symbol> _variables;

    private BlockScope(Scope enclosingScope, ImmutableArray<Symbol> variables)
        : base(enclosingScope)
    {
        _variables = variables;
    }


    /// <summary>
    /// Creates a new block scope without local funs or variables
    /// that inherits flags from <paramref name="enclosingScope"/>.
    /// </summary>
    public BlockScope(Scope enclosingScope)
        : this(enclosingScope, [])
    {
    }

    public BlockScope WithVariable(VariableSymbol variable)
    {
        Guard.IsState(EnclosingScope is not null, "Block scopes must always have an enclosing scope.");

        // The new variable will shadow those with the same name.

        var adjacentVars = _variables.Where(v => v.Name != variable.Name);
        return new BlockScope(EnclosingScope, [.. adjacentVars, variable]);
    }

    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        => _variables;
}