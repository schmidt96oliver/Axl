namespace Axl.Compiler.Semantics.Symbols;

public sealed class VariableSymbol(SymbolName name, TypeSymbol type) : Symbol(name)
{
    public TypeSymbol Type { get; } = type;
}