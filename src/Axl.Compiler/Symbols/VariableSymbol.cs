namespace Axl.Compiler.Symbols;

public sealed class VariableSymbol(string name, TypeSymbol type) : Symbol(name)
{
    public override SymbolKind Kind => SymbolKind.Variable;
    
    public TypeSymbol Type { get; } = type;
}