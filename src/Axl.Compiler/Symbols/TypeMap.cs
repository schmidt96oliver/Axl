using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

/// <summary>
/// A mapping of type parameters to their substituted types. If <paramref name="strict"/>
/// is true, every type parameters must map to a concrete type (i.e. not a type parameter).
/// Otherwise, it can be another type parameter or not be present.
/// </summary>
public sealed class TypeMap(FrozenDictionary<TypeParameterSymbol, TypeSymbol> map, bool strict)
{
    public TypeSymbol Get(TypeParameterSymbol typeParam)
        => TryGet(typeParam) ?? throw new InvalidOperationException(
            $"The type '{typeParam.FullName}' is not defined in this {nameof(TypeMap)}.");
    
    public TypeSymbol? TryGet(TypeParameterSymbol typeParam)
    {
        if (map.TryGetValue(typeParam, out var type))
        {
            if (strict && type is TypeParameterSymbol)
            {
                throw new InvalidOperationException(
                    $"The type '{typeParam.FullName}' maps to a type parameter again: '{type.FullName}'.");
            }

            return type;
        }

        if (strict)
        {
            throw new InvalidOperationException(
                $"The type '{typeParam.FullName}' is not defined in this {nameof(TypeMap)}.");
        }

        return null;
    }

    public TypeSymbol ReplaceOrKeep(TypeSymbol type)
        => type is TypeParameterSymbol typeParam && TryGet(typeParam) is { } replaced ? replaced : type;
    
    public static TypeMap StrictFrom(ImmutableArray<TypeParameterSymbol> typeParameters,
        ImmutableArray<TypeSymbol> substitutedTypes)
    {
        Guard.MustBe(typeParameters.Length == substitutedTypes.Length);
        Guard.MustBe(substitutedTypes.All(t => t is not TypeParameterSymbol));

        return new TypeMap(Enumerable
            .Range(0, typeParameters.Length)
            .ToDictionary(keySelector: i => typeParameters[i],
                elementSelector: i => substitutedTypes[i])
            .ToFrozenDictionary(), strict: true);
    }

    public static TypeMap From(ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeSymbol>
        substitutedTypes)
    {
        Guard.MustBe(typeParameters.Length == substitutedTypes.Length);

        return new TypeMap(Enumerable
            .Range(0, typeParameters.Length)
            .ToDictionary(keySelector: i => typeParameters[i],
                elementSelector: i => substitutedTypes[i])
            .ToFrozenDictionary(), strict: false);
    }
}