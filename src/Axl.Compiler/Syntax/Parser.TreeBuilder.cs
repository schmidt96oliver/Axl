using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private FileSyntax BuildTree(ImmutableArray<Token> tokens, DiagnosticBag diagnosticBag)
    {
        Stack<ImmutableArray<SyntaxElement>.Builder> nodeBuilders = [];
        var nextToken = 0;
        
        var lastClaimedError = new ClaimedRange(-1, -1);
        var sawErrorElement = false;
        
        foreach (var e in _scanner.GetEvents())
        {
            switch (e)
            {
                case ParseEvent.Open:
                    nodeBuilders.Push(ImmutableArray.CreateBuilder<SyntaxElement>());
                    break;

                case ParseEvent.Eat:
                case ParseEvent.EatAs:
                    // Flush all trivia here
                    while (nextToken < tokens.Length && tokens[nextToken].Kind.IsTrivia)
                    {
                        nodeBuilders.Peek().Add(tokens[nextToken]);
                        nextToken++;
                    }

                    // Add the actual node
                    var token = e is ParseEvent.EatAs eatAsEvent
                        ? tokens[nextToken].WithKind(eatAsEvent.Kind)
                        : tokens[nextToken];

                    nodeBuilders.Peek().Add(token);

                    // Advance
                    nextToken++;
                    break;

                case ParseEvent.Make(var kind):
                    var span = nextToken == 0
                        ? SourceSpan.EmptyBefore(tokens[0].FullSpan)
                        : SourceSpan.EmptyAfter(tokens[nextToken - 1].FullSpan);

                    nodeBuilders.Peek().Add(Token.MakeMissing(span, kind));

                    sawErrorElement = true;
                    break;


                case ParseEvent.Close { Kind: SyntaxKind.File }:
                {
                    Debug.Assert(nodeBuilders.Count == 1, "File was not the root.");
                    Debug.Assert(nextToken == tokens.Length, "File did not eat all tokens.");

                    var rootNode = new FileSyntax(nodeBuilders.Pop().DrainToImmutable());
                    
                    // Explicitly set its parent, because there is no parent node
                    // to do it. Normally they are set in syntax node constructor.
                    rootNode.Parent = null;
                    
                    Debug.Assert(sawErrorElement == diagnosticBag.HasError,
                        sawErrorElement
                            ? "Saw error element(s), but no diagnostics."
                            : "Saw no error element(s), but reported an error.");
                    return rootNode;
                }

                case ParseEvent.Close(var kind):
                {
                    var nodeBuilder = nodeBuilders.Pop();
                    var syntaxNode = SyntaxNodeFactory.Create(kind, nodeBuilder.DrainToImmutable());
                    nodeBuilders.Peek().Add(syntaxNode);

                    if (kind is SyntaxKind.Garbage or SyntaxKind.ErrorExpr)
                        sawErrorElement = true;
                    
                    break;
                }

                case ParseEvent.Report { Error: var error, ClaimedRange: var range, IsSuppressible: true }:
                {
                    Debug.Assert(range.First >= lastClaimedError.First, "Error claimed ranges are not sequential");

                    if (range.First > lastClaimedError.Last)
                    {
                        diagnosticBag.ReportError(error);
                        lastClaimedError = range;
                    }

                    break;
                }

                case ParseEvent.Report { Error: var error, IsSuppressible: false }:
                    diagnosticBag.ReportError(error);
                    break;
            }
        }

        throw new UnreachableException("Event stream ended without closing File.");
    }
}