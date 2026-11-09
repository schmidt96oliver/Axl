using System.Collections.Immutable;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public closed class FieldSymbol(string name, TypeSymbol owner, bool isPublic) 
    : Symbol(owner.Compilation, name, owner)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;
    
    public override bool IsPublic { get; } = isPublic;

    /// <inheritdoc />
    public override bool IsConcrete => Owner!.IsConcrete && Type.IsConcrete;

    public abstract TypeSymbol Type { get; }

    public abstract override FieldDefinitionSymbol OriginalDefinition { get; }

    public override FieldSymbol Substitute(TypeMap map)
        => OriginalDefinition.Specialize(owner.Substitute(map));

}