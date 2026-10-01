namespace Axl.Compiler.Symbols;

public class VariableSymbol : Symbol
{
    public override SymbolKind Kind => SymbolKind.Variable;

    public bool IsReadOnly { get; }
    public TypeSymbol Type { get; }

    public FunSymbol Owner
    {
        get
        {
            Guard.IsState(field is not null, "Owner has not been set.");
            return field;
        }
        set
        {
            Guard.IsState(field is null, "Owner has already been set.");
            field = value;
        }
    }

    
    /// <summary>
    /// Leaves <see cref="Owner"/> unassigned, so it must be assigned manually.
    /// </summary>
    protected VariableSymbol(string name, bool isReadOnly, TypeSymbol type)
        : base(name)
    {
        IsReadOnly = isReadOnly;
        Type = type;
    }

    public VariableSymbol(string name, bool isReadOnly, TypeSymbol type, FunSymbol owner) 
        : this(name, isReadOnly, type)
    {
        Owner = owner;
    }
}