using System.Collections.Immutable;
using Axl.Compiler.Binding;

namespace Axl.Compiler.Symbols;

public closed class TypeSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic, PrimitiveKind? primitiveKind = null)
    : NamespaceOrTypeSymbol(compilation, name, owner)
{
    public override bool IsPublic { get; } = isPublic;
    
    /// <inheritdoc />
    public override bool IsConcrete
        => Owner?.IsConcrete != false && TypeArguments.All(arg => arg.IsConcrete);

    public override TypeSymbol OriginalDefinition => this;

    public PrimitiveKind? PrimitiveKind { get; } = primitiveKind;

    public bool IsPrimitive => PrimitiveKind is not null;

    public abstract ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

    /// <summary>
    /// The type arguments that fill <see cref="TypeParameters"/>. In their
    /// initial state, <see cref="TypeArguments"/> is filled with the parameters
    /// themselves. Might be specialized.
    /// </summary>
    public abstract ImmutableArray<TypeSymbol> TypeArguments { get; }

    public LayoutGraphNode LayoutGraphNode
    {
        get
        {
            field ??= new LayoutGraphNode(this);
            return field;
        }
    }


    public override TypeSymbol Substitute(TypeMap map)
    {
        if (OriginalDefinition is not TypeDefinitionSymbol originalDefinition)
        {
            throw new InvalidOperationException(
                $"Cannot substitute here without {nameof(TypeDefinitionSymbol)}. This should be overwritten.");
        }

        var specializedOwner = Owner?.Substitute(map);
        var specializedArgs = TypeArguments.Select(arg => arg.Substitute(map)).ToImmutableArray();
        return originalDefinition.Specialize(specializedOwner, specializedArgs);
    }
    
    public TypeSymbol Specialize(ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.MustBe(TypeArguments.SequenceEqual(TypeParameters), "For specialization, the type must still be open.");
        Guard.MustBe(typeArguments.Length == TypeParameters.Length);
        if (typeArguments.Length == 0)
            return this;
        
        if (OriginalDefinition is not TypeDefinitionSymbol originalDefinition)
            throw new InvalidOperationException($"Cannot specialize {this.GetType().Name}");

        return originalDefinition.Specialize(Owner, typeArguments);
    }
}