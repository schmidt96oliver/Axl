using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Symbols;

/// <summary>
/// The fun that is created by substituting all of its own type
/// parameters with concrete type arguments.
/// </summary>
public sealed class ConstructedFunSymbol : FunSymbol
{
    public ConstructedFunSymbol(FunSymbol genericFunSymbol, ImmutableArray<TypeSymbol> typeArguments) 
        : base(genericFunSymbol.Compilation, genericFunSymbol.Name, genericFunSymbol.Owner, genericFunSymbol.IsPublic)
    {
        OriginalDefinition = genericFunSymbol.OriginalDefinition;
        ConstructedFrom = genericFunSymbol;
        TypeArguments = typeArguments;
        
        Debug.Assert(TypeArguments.Length == TypeParameters.Length);
    }
    
    /// <inheritdoc />
    public override bool CanBeReferencedByName => OriginalDefinition.CanBeReferencedByName;

    /// <inheritdoc />
    public override FunDefinitionSymbol OriginalDefinition { get; }
    
    /// <inheritdoc />
    public override FunSymbol ConstructedFrom { get; }

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => ConstructedFrom!.TypeParameters;

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments { get; }

    /// <inheritdoc />
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (field.IsDefault)
                field = [.. ConstructedFrom!.Parameters.Select(SubstituteParameter)];

            return field;
        }
    }

    private TypeSymbol SubstituteType(TypeSymbol original)
        => original is TypeParameterSymbol typeParameter && TypeParameters.Contains(typeParameter)
            ? TypeArguments[typeParameter.Ordinal]
            : original;

    private ParameterSymbol SubstituteParameter(ParameterSymbol original)
        => new(original.Name, owner: this, SubstituteType(original.Type), original.IsMut, original.IsSelf);
    

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter
    {
        get
        {
            if (ConstructedFrom.SelfParameter is null)
                return null;

            field ??= SubstituteParameter(ConstructedFrom!.SelfParameter);
            return field;
        }
    }

    /// <inheritdoc />
    public override TypeSymbol ReturnType
    {
        get
        {
            field ??= SubstituteType(ConstructedFrom.ReturnType);
            return field;
        }
    }

    /// <inheritdoc />
    public override bool IsReturnMut => ConstructedFrom.IsReturnMut;
}