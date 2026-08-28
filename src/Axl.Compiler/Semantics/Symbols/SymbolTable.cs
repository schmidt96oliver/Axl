using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class SymbolTable
{
    private readonly FrozenDictionary<MemberSyntax, Symbol> _symbolsBySyntax;
    public ImmutableArray<Symbol> TopLevelSymbols { get; }

    internal SymbolTable(FrozenDictionary<MemberSyntax, Symbol> symbolsBySyntax, ImmutableArray<Symbol> topLevelSymbols)
    {
        _symbolsBySyntax = symbolsBySyntax;
        TopLevelSymbols = topLevelSymbols;
    }
    
    public Symbol GetSymbol(MemberSyntax node)
    {
        return _symbolsBySyntax.TryGetValue(node, out var symbol)
            ? symbol 
            : throw new ArgumentException($"No symbol found for {node}", nameof(node));
    }
}