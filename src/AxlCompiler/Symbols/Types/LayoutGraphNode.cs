using System.Collections.Immutable;

namespace AxlCompiler.Symbols;

/// <summary>
/// The node that corresponds to a specific type in the layout graph.
/// Used to detect cycles in value-based layout.
/// </summary>
public sealed class LayoutGraphNode(TypeSymbol type)
{
    private sealed record Edge(FieldSymbol Field, LayoutGraphNode To);
    
    /// <summary>
    /// For each field that is stored by value, its original definition is an edge.
    /// All type arguments are edges as if they were fields. E.g. a field of
    /// type A[B[I32]] adds edges -> A[T]; -> B[T]; -> I32.
    /// </summary>
    private ImmutableArray<Edge> Edges
    {
        get
        {
            //TODO: Check if a type argument is actually stored by value

            if (!field.IsDefault)
                return field;

            // To avoid more complexity, expect that each type stores its type
            // parameters by value. This might not be the case, e.g. it could not
            // store it at all (`struct Empty[T]`) or by reference (`struct Ref[T] { F: Buffer[T]; }`).
            // This choice is defensive and should mostly work, I think. Let's see.
            
            var edges = ImmutableArray.CreateBuilder<Edge>();
            foreach (var fieldSymbol in type.Members.OfType<FieldSymbol>().DistinctBy(f => f.Type))
            {
                var fieldType = fieldSymbol.Type;
                AddEdges(fieldType, fieldSymbol);
            }

            field = edges.DrainToImmutable();
            return field;

            void AddEdges(TypeSymbol fieldType, FieldSymbol fieldSymbol)
            {
                edges.Add(new Edge(fieldSymbol, To: fieldType.OriginalDefinition.LayoutGraphNode));
                foreach (var typeArg in fieldType.TypeArguments.Distinct())
                    AddEdges(typeArg, fieldSymbol);
            }
        }
    }

    /// <summary>
    /// Returns a path from this node that ends at this node,
    /// if one exists. <c>null</c>, otherwise.
    /// </summary>
    public ImmutableArray<FieldSymbol>? TryGetCyclicPath() => PathFrom(this, []);

    /// <summary>
    /// Returns a path from a start node to this node, if
    /// one exists. <c>null</c>, otherwise. Adds seen nodes
    /// to <paramref name="visited"/> and returns <c>null</c>,
    /// if all edges end in a node that has been seen.
    /// </summary>
    private ImmutableArray<FieldSymbol>? PathFrom(LayoutGraphNode from, HashSet<LayoutGraphNode> visited)
    {
        foreach (var edge in from.Edges)
        {
            if (edge.To == this)
                return [edge.Field];

            if (visited.Add(edge.To))
            {
                if (PathFrom(edge.To, visited) is { } nextPath)
                    return [edge.Field, .. nextPath];
            }
        }

        return null;
    }
}