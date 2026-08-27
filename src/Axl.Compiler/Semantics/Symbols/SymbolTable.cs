using System.Collections.Frozen;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public record SymbolTable(Compilation Compilation, FrozenDictionary<MemberSyntax, Symbol> SymbolsBySyntax, HashSet<Symbol> AllSymbols)
{
    public Symbol GetSymbol(MemberSyntax node)
    {
        return SymbolsBySyntax[node];
    }
}