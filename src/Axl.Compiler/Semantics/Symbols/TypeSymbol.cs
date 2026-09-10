namespace Axl.Compiler.Semantics.Symbols;

public sealed class TypeSymbol(string name) : Symbol(SymbolName.From(name));