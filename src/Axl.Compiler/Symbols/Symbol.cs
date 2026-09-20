namespace Axl.Compiler.Symbols;

public closed class Symbol(string name)
{
    public string Name { get; } = name;

    public abstract SymbolKind Kind { get; }
}