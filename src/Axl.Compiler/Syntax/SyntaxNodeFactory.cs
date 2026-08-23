using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax;

using ElementArray = ImmutableArray<SyntaxElement>;

public static class SyntaxNodeFactory
{
    private static readonly FrozenDictionary<SyntaxKind, Func<ElementArray, SyntaxNode>> FactoriesByKind
        = GetFactoriesByKind(allKinds: Enum.GetValues<SyntaxKind>());

    /// <summary>
    /// Uses reflection to retrieve all subtypes of <see cref="SyntaxNode"/> and map them to their corresponding
    /// <see cref="SyntaxKind"/>.
    /// </summary>
    private static FrozenDictionary<SyntaxKind, Func<ElementArray, SyntaxNode>> GetFactoriesByKind(SyntaxKind[] allKinds)
    {
        Dictionary<SyntaxKind, Func<ElementArray, SyntaxNode>> factories = [];

        var allSubTypes = typeof(SyntaxNode).Assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(SyntaxNode)));
        
        foreach (var subType in allSubTypes)
        {
            // Subtypes without children-only constructor are not suitable.
            var suitableConstructor = subType.GetConstructor([typeof(ElementArray)]);
            if (suitableConstructor is null)
                continue;

            var kind = GetKind(subType, allKinds);
            factories.Add(kind, array => (SyntaxNode)suitableConstructor.Invoke([array]));
        }

        return factories.ToFrozenDictionary();
    }
    
    private static SyntaxKind GetKind(Type type, SyntaxKind[] allKinds)
    {
        var matchingKinds = allKinds.Where(k => type.Name == $"{k}Syntax").ToList();
        if (matchingKinds is not [var kind])
        {
            throw new ArgumentException(
                $"Leaf-subtype {type.Name} of {nameof(SyntaxNode)} has no name that corresponds to {nameof(SyntaxKind)} enum.",
                nameof(type));
        }

        return kind;
    }
    
    
    public static SyntaxNode Create(SyntaxKind kind, ElementArray children)
    {
        if (FactoriesByKind.TryGetValue(kind, out var constructor))
        {
            var node = constructor(children);
            Debug.Assert(node.Kind == kind);
            return node;
        }
        return new SyntaxNode(kind, children);
    }
}