namespace Axl.Compiler.Symbols;

public sealed class ParameterSymbol(string name, TypeSymbol type) 
    : VariableSymbol(name, isReadOnly: true, type)
{
    public override SymbolKind Kind => SymbolKind.Parameter;
}