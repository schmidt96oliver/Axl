using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private record BuildingNode(SyntaxKind Kind, ImmutableArray<SyntaxElement>.Builder Nodes);

    private SyntaxTree BuildTree()
    {
        //TODO: Add good trivia logic here

        Stack<BuildingNode> nodes = [];
        var tokens = _scanner.AllTokens;
        var nextToken = 0;
        foreach (var e in _scanner.GetEvents())
        {
            switch (e.EventKind)
            {
                case ParseEventKind.Open:
                    Debug.Assert(e.SyntaxKind is not null, "Unclosed node");

                    nodes.Push(new BuildingNode(e.SyntaxKind.Value, ImmutableArray.CreateBuilder<SyntaxElement>()));
                    break;

                case ParseEventKind.Eat:
                case ParseEventKind.EatAs:
                    // Flush all trivia here
                    while (tokens[nextToken].Kind.IsTrivia)
                    {
                        nodes.Peek().Nodes.Add(tokens[nextToken]);
                        nextToken++;
                    }

                    // Add the actual node
                    var token = e.EventKind is ParseEventKind.Eat
                        ? tokens[nextToken]
                        : tokens[nextToken].WithKind(e.TokenKind
                                                     ?? throw new UnreachableException("AdvancePatch without kind."));

                    nodes.Peek().Nodes.Add(token);
                    nextToken++;
                    break;
                
                case ParseEventKind.Make:
                    var span = nextToken == 0
                        ? SourceSpan.EmptyBefore(tokens[0].Span)
                        : SourceSpan.EmptyAfter(tokens[nextToken - 1].Span);
                    
                    nodes.Peek().Nodes.Add(Token.MakeMissing(
                        span,
                        kind: e.TokenKind ?? throw new UnreachableException("CreateMissing event without kind.")));
                    break;
                    

                case ParseEventKind.Close:
                    var builtNode = nodes.Pop();
                    var isRoot = builtNode.Kind is SyntaxKind.TreeRoot;
                    if (isRoot)
                    {
                        Debug.Assert(nodes.Count == 0, "TreeRoot was not the root.");
                        Debug.Assert(nextToken == tokens.Length, "TreeRoot did not eat all tokens.");
                    }

                    var node = new SyntaxNode(builtNode.Kind, builtNode.Nodes.DrainToImmutable());

                    if (isRoot)
                    {
                        return new SyntaxTree(
                            root: node,
                            diagnostics: _errorContext.Bag.Drain(),
                            hasError: _errorContext.Bag.HasError);
                    }

                    nodes.Peek().Nodes.Add(node);
                    break;
            }
        }

        throw new UnreachableException();
    }
}