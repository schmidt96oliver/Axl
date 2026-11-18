using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

/// <summary>
/// Everything about a declaration that is bound once and never substituted:
/// syntax, type parameters, members, signatures, bodies. A definition is not
/// a <see cref="Symbol"/>. Symbols are instances of a definition (identity
/// or specialized) and are created only through <see cref="Specialize"/>.
/// </summary>
public closed class Definition<TSymbol>(Compilation compilation, string name, Symbol? owner, bool isPublic)
    where TSymbol : Symbol
{
    private readonly List<(Symbol? Owner, ImmutableArray<TypeSymbol> TypeArguments, TSymbol Symbol)> _specializations = [];

    public Compilation Compilation { get; } = compilation;

    public string Name { get; } = name;

    /// <summary>
    /// The identity symbol this definition is declared in.
    /// </summary>
    public Symbol? Owner { get; } = owner;

    public bool IsPublic { get; } = isPublic;

    public virtual bool CanBeReferencedByName => true;

    public abstract ImmutableArray<SyntaxNode> DeclaringSyntax { get; }

    public abstract ImmutableArray<Token> NameSyntax { get; }

    public virtual ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    /// <summary>
    /// The unspecialized symbol: owned by <see cref="Owner"/>, type arguments
    /// are the definition's own type parameters.
    /// </summary>
    public TSymbol Identity
    {
        get
        {
            // The identity cannot be keyed by its type arguments eagerly, because
            // binding type parameters needs a scope owned by the identity.
            field ??= Create(Owner, typeArguments: null);
            return field;
        }
    }

    public TSymbol Specialize(Symbol? owner, ImmutableArray<TypeSymbol> typeArguments)
    {
        Guard.MustBe(ReferenceEquals(owner?.OriginalDefinition, Owner?.OriginalDefinition));
        Guard.MustBe(typeArguments.Length == TypeParameters.Length);

        if (Equals(owner, Owner) && typeArguments.SequenceEqual(TypeParameters.CastArray<TypeSymbol>()))
            return Identity;

        foreach (var specialization in _specializations)
        {
            if (Equals(specialization.Owner, owner) && specialization.TypeArguments.SequenceEqual(typeArguments))
                return specialization.Symbol;
        }

        var symbol = Create(owner, typeArguments);
        _specializations.Add((owner, typeArguments, symbol));
        return symbol;
    }

    /// <summary>
    /// Specializes with a new owner, keeping the own type parameters open.
    /// </summary>
    public TSymbol WithOwner(Symbol? owner)
        => Specialize(owner, TypeParameters.CastArray<TypeSymbol>());

    public virtual void Complete(BinderSideEffects sideEffects)
    {
    }

    /// <param name="typeArguments"><c>null</c> for the identity.</param>
    protected abstract TSymbol Create(Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments);
}