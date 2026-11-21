using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding.Scopes;

public sealed class BlockScope : Scope
{
    private readonly ImmutableArray<Symbol> _locals;

    private BlockScope(Scope enclosingScope, ImmutableArray<Symbol> locals)
        : base(enclosingScope)
    {
        _locals = locals;
    }


    /// <summary>
    /// Creates a new block scope without local funs or variables
    /// that inherits flags from <paramref name="enclosingScope"/>.
    /// </summary>
    public BlockScope(Scope enclosingScope)
        : this(enclosingScope, [])
    {
    }

    public BlockScope WithLocal(LocalSymbol local)
    {
        Guard.IsState(EnclosingScope is not null, "Block scopes must always have an enclosing scope.");

        // The new local will shadow those with the same name.

        var adjacentVars = _locals.Where(v => v.Name != local.Name);
        return new BlockScope(EnclosingScope, [.. adjacentVars, local]);
    }

    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        => _locals;
}