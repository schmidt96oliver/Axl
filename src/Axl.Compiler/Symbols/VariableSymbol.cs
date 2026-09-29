namespace Axl.Compiler.Symbols;

public class VariableSymbol(string name, bool isReadOnly, TypeSymbol type) : Symbol(name)
{
    public override SymbolKind Kind => SymbolKind.Variable;

    public bool IsReadOnly { get; } = isReadOnly;
    public TypeSymbol Type { get; } = type;
}