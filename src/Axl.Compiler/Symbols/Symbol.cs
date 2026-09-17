namespace Axl.Compiler.Symbols;

public closed class Symbol(SymbolName name)
{
    public SymbolName Name { get; } = name;

    public abstract string KindName { get; }
}