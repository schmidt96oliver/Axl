using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler;

public enum UnresolvedSymbolReason
{
    NotFound,
    Ambiguous,
    NotUsable,
    NotAccessible,
    NoApplicableType,
    NoApplicableOverload,
}

public closed record SymbolInfo
{
    /// <summary>
    /// The meaning of the reference is clear and would bind correctly.
    /// Note that this doesn't mean that the surrounding code binds correctly.
    /// </summary>
    public sealed record Resolved(Symbol Symbol) : SymbolInfo;

    /// <summary>
    /// The meaning of the reference is unclear. Given the possible
    /// <see cref="Candidates"/> including a <see cref="Reason"/>, why
    /// the reference is not clear. Note that <see cref="Candidates"/>
    /// can contain only a single symbol, because it could not correctly bind.
    /// </summary>
    public sealed record Unresolved(ImmutableArray<Symbol> Candidates, UnresolvedSymbolReason Reason) : SymbolInfo;

    public ImmutableArray<Symbol> Symbols => this switch
    {
        Resolved(var s) => [s],
        Unresolved(var c, _) => c
    };
}