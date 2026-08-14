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
            switch (e)
            {
                case ParseEvent.Open openEvent:
                    Debug.Assert(openEvent.Kind is not null, "Unclosed node");

                    nodes.Push(new BuildingNode(openEvent.Kind.Value, ImmutableArray.CreateBuilder<SyntaxElement>()));
                    break;

                case ParseEvent.Eat:
                case ParseEvent.EatAs:
                    // Flush all trivia here
                    while (nextToken < tokens.Length && tokens[nextToken].Kind.IsTrivia)
                    {
                        nodes.Peek().Nodes.Add(tokens[nextToken]);
                        nextToken++;
                    }

                    // Add the actual node
                    var token = e is ParseEvent.EatAs eatAsEvent
                        ? tokens[nextToken].WithKind(eatAsEvent.Kind)
                        : tokens[nextToken];

                    nodes.Peek().Nodes.Add(token);
                    nextToken++;
                    break;
                
                case ParseEvent.Make(var kind):
                    var span = nextToken == 0
                        ? SourceSpan.EmptyBefore(tokens[0].Span)
                        : SourceSpan.EmptyAfter(tokens[nextToken - 1].Span);
                    
                    nodes.Peek().Nodes.Add(Token.MakeMissing(span, kind));
                    break;
                    

                case ParseEvent.Close:
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