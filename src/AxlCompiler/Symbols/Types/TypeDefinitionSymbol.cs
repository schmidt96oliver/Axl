using System.Collections.Immutable;

namespace AxlCompiler.Symbols;

public closed class TypeDefinitionSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic, PrimitiveKind? primitiveKind = null)
    : TypeSymbol(compilation, name, owner, isPublic, primitiveKind)
{
    private readonly List<SpecializedTypeSymbol> _specializations = [];
    
    public override TypeDefinitionSymbol OriginalDefinition => this;

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments => TypeParameters.CastArray<TypeSymbol>();

    public TypeSymbol Specialize(Symbol? specializedOwner, ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.MustBe(ReferenceEquals(specializedOwner?.OriginalDefinition, Owner?.OriginalDefinition));
        Guard.MustBe(typeArguments.Length == TypeParameters.Length);

        // If nothing changes, just return this definition.
        if (Equals(specializedOwner, Owner) && TypeArguments.SequenceEqual(typeArguments))
            return this;

        var specialization = _specializations.FirstOrDefault(s => Equals(s.Owner, specializedOwner) &&
                                                                  s.TypeArguments.SequenceEqual(typeArguments));
        if (specialization is null)
        {
            specialization = new SpecializedTypeSymbol(
                originalOriginalDefinition: this,
                specializedOwner,
                typeArguments);
            _specializations.Add(specialization);
        }

        return specialization;
    }
}