namespace Axl.Compiler.Symbols;

public abstract class Symbol(SymbolName name)
{
    public SymbolName Name { get; } = name;

    public abstract string KindName { get; }
}