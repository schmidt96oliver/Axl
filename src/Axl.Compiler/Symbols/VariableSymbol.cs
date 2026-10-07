namespace Axl.Compiler.Symbols;

public class VariableSymbol(string name, FunSymbol parent, bool isReadOnly, TypeSymbol type)
    : Symbol(name, parent)
{
    public bool IsReadOnly { get; } = isReadOnly;
    public TypeSymbol Type { get; } = type;

    /// <summary>
    /// Variables are always private inside their fun body.
    /// </summary>
    public override bool IsPublic => false;
}