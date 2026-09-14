namespace Axl.Compiler.Symbols;

public sealed class TypeSymbol(string name) : Symbol(SymbolName.From(name))
{
    public override string KindName => "native type";
}