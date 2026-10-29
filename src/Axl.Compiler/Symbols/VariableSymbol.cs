namespace Axl.Compiler.Symbols;

public class VariableSymbol(string name, FunSymbol owner, bool isReadOnly, TypeSymbol type)
    : Symbol(owner.Compilation, name, owner)
{
    public bool IsReadOnly { get; } = isReadOnly;
    public TypeSymbol Type { get; } = type;

    /// <summary>
    /// Variables are always private inside their fun body.
    /// </summary>
    public override bool IsPublic => false;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;
}