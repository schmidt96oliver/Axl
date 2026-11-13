using System.Collections.Immutable;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

/// <summary>
/// A <see cref="TypeSymbol"/> where owner and/or type arguments have
/// been substituted.
/// </summary>
public class SpecializedTypeSymbol : TypeSymbol
{
    public override TypeDefinitionSymbol OriginalDefinition { get; }

    /// <inheritdoc />
    public override bool CanBeReferencedByName => OriginalDefinition.CanBeReferencedByName;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => OriginalDefinition.DeclaringSyntax;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => OriginalDefinition.NameSyntax;
    
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (field.IsDefault)
            {
                var typeMap = TypeMap.For(this);
                field = [.. OriginalDefinition.Members.Select(m => m.Substitute(typeMap))];
            }

            return field;
        }
    }

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => OriginalDefinition.TypeParameters;

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments { get; }

    /// <summary>
    /// A <see cref="TypeSymbol"/> where owner and/or type arguments have
    /// been substituted.
    /// </summary>
    public SpecializedTypeSymbol(TypeDefinitionSymbol originalOriginalDefinition,
        Symbol? specializedOwner,
        ImmutableArray<TypeSymbol> typeArguments)
        : base(originalOriginalDefinition.Compilation,
            originalOriginalDefinition.Name, specializedOwner, originalOriginalDefinition.IsPublic,
            originalOriginalDefinition.PrimitiveKind)
    {
        Guard.MustBe(ReferenceEquals(originalOriginalDefinition.Owner, specializedOwner?.OriginalDefinition));
        Guard.MustBe(typeArguments.Length == originalOriginalDefinition.TypeParameters.Length);

        OriginalDefinition = originalOriginalDefinition;
        TypeArguments = typeArguments;
    }

    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => other is TypeSymbol otherType &&
           Equals(OriginalDefinition, otherType.OriginalDefinition) &&
           Equals(Owner, otherType.Owner) &&
           TypeArguments.SequenceEqual(otherType.TypeArguments);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(0x33AD_ADBC);
        hash.Add(OriginalDefinition);
        hash.Add(Owner);
        foreach (var arg in TypeArguments)
            hash.Add(arg);
        return hash.ToHashCode();
    }
}