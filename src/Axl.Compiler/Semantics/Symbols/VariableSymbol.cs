using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class VariableSymbol(SymbolName name, AxlType type) : Symbol(name)
{
    public AxlType Type { get; } = type;
}