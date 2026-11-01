using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Axl.Compiler.Symbols;

public sealed class SpecializedFunSymbol : FunSymbol
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => OriginalDefinition.CanBeReferencedByName;

    /// <inheritdoc />
    public override FunDefinitionSymbol OriginalDefinition { get; }

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => OriginalDefinition.TypeParameters;

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments { get; }

    /// <inheritdoc />
    public override bool IsReturnMut => OriginalDefinition.IsReturnMut;

    
    private TypeMap TypeMap
    {
        get
        {
            field ??= TypeMap.For(this);
            return field;
        }
    }
    
    /// <inheritdoc />
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (field.IsDefault)
                field = [.. OriginalDefinition.Parameters.Select(p => p.Substitute(TypeMap))];

            return field;
        }
    }

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter
    {
        get
        {
            if (OriginalDefinition.SelfParameter is null)
                return null;

            field ??= OriginalDefinition.SelfParameter.Substitute(TypeMap);
            return field;
        }
    }

    /// <inheritdoc />
    public override TypeSymbol ReturnType
    {
        get
        {
            field ??= OriginalDefinition.ReturnType.Substitute(TypeMap);
            return field;
        }
    }
    
    
    public SpecializedFunSymbol(FunDefinitionSymbol originalDefinition, 
        Symbol? specializedOwner,
        ImmutableArray<TypeSymbol> typeArguments) 
        : base(originalDefinition.Compilation, originalDefinition.Name, specializedOwner, originalDefinition.IsPublic)
    {
        Guard.MustBe(Equals(originalDefinition.Owner, specializedOwner?.OriginalDefinition));
        Guard.MustBe(typeArguments.Length == originalDefinition.TypeParameters.Length);
        
        OriginalDefinition = originalDefinition.OriginalDefinition;
        TypeArguments = typeArguments;
    }

    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        // Compare by OriginalDefinition + Owner + TypeArguments, so that
        // specialized funs are equal if they have been created with equal
        // arguments.
        => other is FunSymbol otherFun &&
           Equals(otherFun.OriginalDefinition, OriginalDefinition) &&
           Equals(otherFun.Owner, Owner) &&
           otherFun.TypeArguments.SequenceEqual(TypeArguments);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(0x18F2_AAD1);
        hash.Add(RuntimeHelpers.GetHashCode(OriginalDefinition));
        hash.Add(Owner);
        foreach (var typeArg in TypeArguments)
            hash.Add(typeArg);
        return hash.ToHashCode();
    }
}