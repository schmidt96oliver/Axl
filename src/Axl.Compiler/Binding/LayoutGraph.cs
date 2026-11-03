using System.Collections.Immutable;
using System.Text;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding;

public sealed class LayoutGraph
{
    public sealed record Edge(Node From, FieldSymbol Field, Node To)
    {
        public override string ToString()
            => $"{From} -- {Field.Name} --> {To}";
    }

    public sealed record Path(Node From, ImmutableArray<Edge> Edges)
    {
        public IEnumerable<Node> Nodes => [From, .. Edges.Select(e => e.To)];
        
        public IEnumerable<FieldSymbol> Fields => Edges.Select(e => e.Field);
        
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(From);
            foreach (var edge in Edges)
            {
                builder.Append($" -- {edge.Field.Name} --> {edge.To}");
            }

            return builder.ToString();
        }
    }
    
    public sealed class Node(LayoutGraph graph, TypeSymbol type)
    {
        public TypeSymbol Type { get; } = type;

        public ImmutableArray<Edge> Edges
        {
            get
            {
                //TODO: Check if a type argument is actually stored by value
                
                if (!field.IsDefault)
                    return field;
                
                var edges = ImmutableArray.CreateBuilder<Edge>();
                foreach (var fieldSymbol in Type.Members.OfType<FieldSymbol>().DistinctBy(f => f.Type))
                {
                    var fieldType = fieldSymbol.Type;
                    AddEdges(fieldType, fieldSymbol);
                }

                field = edges.DrainToImmutable();
                return field;

                void AddEdges(TypeSymbol type, FieldSymbol throughField)
                {
                    edges.Add(new Edge(From: this, throughField, To: graph.NodeFor(type.OriginalDefinition)));
                    foreach (var typeArg in type.TypeArguments.Distinct())
                        AddEdges(typeArg, throughField);
                }
            }
        }

        /// <inheritdoc />
        public override string ToString()
            => Type.FullName;
    }


    private readonly Dictionary<TypeSymbol, Node> _nodes = [];
    private readonly Dictionary<(Node From, Node To), Path?> _paths = [];

    public Node NodeFor(TypeSymbol type)
    {
        if (_nodes.TryGetValue(type, out var node))
            return node;

        node = new Node(this, type);
        _nodes[type] = node;
        return node;
    }

    public Path? TryGetPath(Node from, Node to, Path? inProgress = null)
    {
        if (_paths.TryGetValue((from, to), out var path))
            return path;

        foreach (var edge in from.Edges)
        {
            if (edge.To == to)
            {
                path = new Path(from, [edge]);
                break;
            }

            if (inProgress?.Nodes.Contains(edge.To) == true)
                continue;

            var prependInProgress = new Path(inProgress?.From ?? from, [..inProgress?.Edges ?? [], edge]);
            
            if (TryGetPath(edge.To, to, prependInProgress) is { } nextPath)
            {
                path = new Path(from, [edge, .. nextPath.Edges]);
                break;
            }
        }

        _paths[(from, to)] = path;
        return path;
    }

    public Path? TryGetCyclicPath(TypeSymbol type)
    {
        var node = NodeFor(type);
        return TryGetPath(from: node, to: node);
    }
}