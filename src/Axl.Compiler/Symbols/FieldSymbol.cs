namespace Axl.Compiler.Symbols;

public sealed class FieldSymbol(string name, bool isPub, TypeSymbol type) : Symbol(name)
{
    public bool IsPub { get; } = isPub;
    
    public TypeSymbol Type { get; } = type;
}