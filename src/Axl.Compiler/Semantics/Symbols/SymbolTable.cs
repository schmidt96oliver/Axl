using System.Collections.Frozen;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Symbols;

public record SymbolTable(Compilation Compilation, FrozenDictionary<SyntaxNode, Symbol> SymbolsBySyntax, HashSet<Symbol> AllSymbols)
{
    public Symbol GetSymbol(SyntaxNode node)
    {
        return SymbolsBySyntax[node];
    }
}