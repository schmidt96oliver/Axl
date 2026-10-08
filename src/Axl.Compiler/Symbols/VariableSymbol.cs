namespace Axl.Compiler.Symbols;

public class VariableSymbol(string name, FunSymbol owner, bool isReadOnly, TypeSymbol type)
    : Symbol(name, owner)
{
    public bool IsReadOnly { get; } = isReadOnly;
    public TypeSymbol Type { get; } = type;

    /// <summary>
    /// Variables are always private inside their fun body.
    /// </summary>
    public override bool IsPublic => false;
}