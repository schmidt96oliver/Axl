using System.Collections.Immutable;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public sealed class SpecializedConstSymbol : ConstSymbol
{
    public SpecializedConstSymbol(ConstDefinitionSymbol originalDefinition, Symbol specializedOwner) 
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
    public override ConstDefinitionSymbol OriginalDefinition { get; }

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => OriginalDefinition.DeclaringSyntax;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => OriginalDefinition.NameSyntax;
    
    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => other is ConstSymbol otherConst &&
           Equals(OriginalDefinition, otherConst.OriginalDefinition) &&
           Equals(Owner, otherConst.Owner);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(0x35AF_ADBC, Owner, OriginalDefinition);
}