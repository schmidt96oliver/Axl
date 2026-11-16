using AxlCompiler.Syntax.Tree;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public enum PlaceKind
{
    /// <summary>
    /// A temporary value, not a place.
    /// E.g. "1 + 2" or "Call()" for non-mut returning "Call".
    /// </summary>
    Value,
    
    /// <summary>
    /// A place that can only be read.
    /// E.g. let variable, non-mut parameter or self in non-mut fun.
    /// </summary>
    ReadOnlyPlace,

    /// <summary>
    /// A place that can be mutated and be assigned to
    /// mut parameters and mut return values.
    /// E.g. var variable, mut parameter, self in mut fun.
    /// </summary>
    MutPlace,
}



public closed class BoundValue(SyntaxNode? syntax = null) : BoundStmt(syntax)
{
    // Can be overwritten, so access stays lazy. That has caused some cycle bugs
    // before, where type of the symbol is accessed, while the type of that symbol
    // is being bound (e.g. `const A: A`).
    public abstract TypeSymbol Type { get; }

    public virtual PlaceKind PlaceKind => PlaceKind.Value;

    /// <summary>
    /// If this value is a place, returns the <see cref="BoundValue"/> where
    /// the place started. <c>null</c>, if this value is not a place. The
    /// root can be self, parameter, variable or call to a mut returning fun.
    /// </summary>
    public virtual BoundValue? PlaceRoot => null;
}