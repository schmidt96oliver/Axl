using System.Collections.Immutable;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public sealed class SpecializedFieldSymbol : FieldSymbol
{
    public SpecializedFieldSymbol(FieldDefinitionSymbol originalDefinition, TypeSymbol specializedOwner) 
        : base(originalDefinition.Name, specializedOwner, originalDefinition.IsPublic)
    {
        Guard.MustBe(ReferenceEquals(originalDefinition.Owner, specializedOwner.OriginalDefinition));

        OriginalDefinition = originalDefinition;
    }

    /// <inheritdoc />
    public override TypeSymbol Type
    {
        get
        {
            field ??= OriginalDefinition.Type.Substitute(TypeMap.For(this));
            return field;
        }
    }

    /// <inheritdoc />
    public override FieldDefinitionSymbol OriginalDefinition { get; }

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => OriginalDefinition.DeclaringSyntax;
    
    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => other is FieldSymbol otherField &&
           Equals(OriginalDefinition, otherField.OriginalDefinition) &&
           Equals(Owner, otherField.Owner);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(0x33AD_ADBC, Owner, OriginalDefinition);
}