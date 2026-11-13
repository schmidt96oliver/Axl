using AxlCompiler.Binding.BoundTree;

namespace AxlCompiler.Symbols;

public closed class LocalSymbol(string name, FunSymbol owner)
    : Symbol(owner.Compilation, name, owner)
{
    public override FunSymbol Owner => owner;
    
    public abstract PlaceKind PlaceKind { get; }
    
    public abstract TypeSymbol Type { get; }

    /// <inheritdoc />
    public override bool IsConcrete => Type.IsConcrete;

    /// <summary>
    /// Locals can always be written in source code.
    /// </summary>
    public override bool CanBeReferencedByName => true;

    /// <summary>
    /// Locals are always private inside their fun body.
    /// </summary>
    public override bool IsPublic => false;
}