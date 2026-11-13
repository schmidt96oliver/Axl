using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;

namespace AxlCompiler.Symbols;

/// <summary>
/// A mapping of type parameters to their substituted types. If the map is strict,
/// every type parameters must map to a concrete type.
/// Otherwise, it can be another type parameter or not be present.
/// </summary>
public sealed class TypeMap
{
    private readonly FrozenDictionary<TypeParameterSymbol, TypeSymbol> _map;
    private readonly bool _strict;

    private TypeMap(FrozenDictionary<TypeParameterSymbol, TypeSymbol> map, bool strict)
    {
        _map = map;
        _strict = strict;
    }

    /// <summary>
    /// Replaces the given <paramref name="type"/> if it is defined in this
    /// map, otherwise returns <paramref name="type"/>.
    /// If this <see cref="TypeMap"/> is strict and <paramref name="type"/> is
    /// a <see cref="TypeParameterSymbol"/>, throws if it cannot be replaced
    /// with a concrete type.
    /// </summary>
    public TypeSymbol Map(TypeSymbol type)
    {
        if (type is not TypeParameterSymbol typeParam)
            return type;

        if (!_map.TryGetValue(typeParam, out var replacedType))
        {
            if (_strict)
            {
                throw new InvalidOperationException(
                    $"The type '{typeParam.FullName}' is not defined in this {nameof(TypeMap)}.");
            }

            return type;
        }

        if (_strict && !replacedType.IsConcrete)
        {
            throw new InvalidOperationException(
                $"The type '{typeParam.FullName}' maps to a generic type again: '{replacedType.FullName}'.");
        }

        return replacedType;
    }
    
    public static TypeMap For(Symbol symbol, bool strict = false)
    {
        var map = new Dictionary<TypeParameterSymbol, TypeSymbol>();
        foreach (var ancestor in symbol.SelfAndOwners())
        {
            Add(ancestor switch
            {
                TypeSymbol type => (type.TypeParameters, type.TypeArguments),
                FunSymbol fun => (fun.TypeParameters, fun.TypeArguments),
                
                NamespaceSymbol => ([], []),
                LocalSymbol => ([], []),
                FieldSymbol => ([], []),
            });
        }
        return new TypeMap(map.ToFrozenDictionary(), strict);

        void Add((ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeSymbol> typeArguments) mapping)
        {
            Debug.Assert(mapping.typeParameters.Length == mapping.typeArguments.Length);
            for (var i = 0; i < mapping.typeParameters.Length; i++)
            {
                var param = mapping.typeParameters[i];
                var arg = mapping.typeArguments[i];
                
                // Don't add identity to keep the map clean.
                if (Equals(param, arg)) 
                    continue;
                if (!map.TryAdd(param, arg))
                    throw new UnreachableException("The same type parameter appears over multiple ancestor symbols.");
            }
        }
    }
}