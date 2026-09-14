namespace Axl.Compiler.Symbols;

public sealed class VariableSymbol(SymbolName name, TypeSymbol type) : Symbol(name)
{
    public TypeSymbol Type { get; } = type;
    public override string KindName => "variable";
}