using System.Collections.Immutable;

namespace AxlCompiler.Symbols;

public closed class TypeDefinition(Compilation compilation, string name, Symbol? owner, bool isPublic, bool isPrimitive)
    : Definition<NamedTypeSymbol>(compilation, name, owner, isPublic)
{
    public bool IsPrimitive { get; } = isPrimitive;

    public abstract bool IsValueType { get; }
    public abstract bool IsReferenceType { get; }

    /// <summary>
    /// Members as identity symbols, owned by <see cref="Definition{TSymbol}.Identity"/>.
    /// </summary>
    public abstract ImmutableArray<Symbol> Members { get; }

    protected override NamedTypeSymbol Create(Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments)
        => new(this, owner, typeArguments);
}