using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public closed class FunDefinitionSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic)
    : FunSymbol(compilation, name, owner, isPublic)
{
    private readonly List<SpecializedFunSymbol> _specializations = [];
    
    // This symbol IS the original definition.
    /// <inheritdoc />
    public override FunDefinitionSymbol OriginalDefinition => this;
    
    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments => TypeParameters.CastArray<TypeSymbol>();

    public abstract FunDeclSyntax? DeclarationSyntax { get; }

    public abstract FunBody Body { get; }

    public FunSymbol Specialize(Symbol? specializedOwner, ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.MustBe(Equals(specializedOwner?.OriginalDefinition, Owner?.OriginalDefinition));
        Guard.MustBe(typeArguments.Length == TypeParameters.Length);

        // If nothing changes, just return this definition.
        if (Equals(specializedOwner, Owner) && TypeArguments.SequenceEqual(typeArguments))
            return this;

        var specialization = _specializations.FirstOrDefault(s => Equals(s.Owner, specializedOwner) &&
                                                                  s.TypeArguments.SequenceEqual(typeArguments));
        if (specialization is null)
        {
            specialization = new SpecializedFunSymbol(
                originalDefinition: this,
                specializedOwner,
                typeArguments);
            _specializations.Add(specialization);
        }

        return specialization;
    }
}