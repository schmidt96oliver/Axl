namespace AxlCompiler.Symbols;

public closed class ConstSymbol(string name, Symbol owner, bool isPublic) 
    : Symbol(owner.Compilation, name, owner)
{
    /// <inheritdoc />
    public override Symbol Owner { get; } = owner;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;
    
    public override bool IsPublic { get; } = isPublic;

    /// <inheritdoc />
    public override bool IsConcrete => Owner.IsConcrete && Type.IsConcrete;

    public abstract TypeSymbol Type { get; }

    public abstract override ConstDefinitionSymbol OriginalDefinition { get; }

    public override ConstSymbol Substitute(TypeMap map)
        => OriginalDefinition.Specialize(Owner.Substitute(map));

}