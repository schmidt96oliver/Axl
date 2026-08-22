using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private SyntaxTree BuildTree(ImmutableArray<Token> tokens, DiagnosticBag diagnosticBag)
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
                        ? SourceSpan.EmptyBefore(tokens[0].Span)
                        : SourceSpan.EmptyAfter(tokens[nextToken - 1].Span);

                    nodeBuilders.Peek().Add(Token.MakeMissing(span, kind));

                    sawErrorElement = true;
                    break;


                case ParseEvent.Close { Kind: SyntaxKind.TreeRoot }:
                {
                    Debug.Assert(nodeBuilders.Count == 1, "TreeRoot was not the root.");
                    Debug.Assert(nextToken == tokens.Length, "TreeRoot did not eat all tokens.");

                    var rootNode = new SyntaxNode(SyntaxKind.TreeRoot, nodeBuilders.Pop().DrainToImmutable());
                    
                    Debug.Assert(sawErrorElement == diagnosticBag.HasError,
                        sawErrorElement
                            ? "Saw error element(s), but no diagnostics."
                            : "Saw no error element(s), but reported an error.");
                    return new SyntaxTree(
                        root: rootNode,
                        _source,
                        diagnostics: diagnosticBag.Drain(),
                        hasError: diagnosticBag.HasError);
                }

                case ParseEvent.Close(var kind):
                {
                    var nodeBuilder = nodeBuilders.Pop();
                    var syntaxNode = CreateNode(kind, nodeBuilder.DrainToImmutable());
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

        throw new UnreachableException("Event stream ended without closing TreeRoot.");
    }

    private SyntaxNode CreateNode(SyntaxKind kind, ImmutableArray<SyntaxElement> elements)
        => kind switch
        {
            SyntaxKind.IdName => new IdNameSyntax(elements),
            SyntaxKind.NativeTypeName => new NativeTypeNameSyntax(elements),
            SyntaxKind.QualifiedName => new QualifiedNameSyntax(elements),
            SyntaxKind.BinaryExpr => new BinaryExprSyntax(elements),
            SyntaxKind.UnaryExpr => new UnaryExprSyntax(elements),
            SyntaxKind.GroupExpr => new GroupExprSyntax(elements),
            SyntaxKind.CallExpr => new CallExprSyntax(elements),
            SyntaxKind.GetMemberExpr => new GetMemberExprSyntax(elements),
            SyntaxKind.BreakExpr => new BreakExprSyntax(elements),
            SyntaxKind.ContinueExpr => new ContinueExprSyntax(elements),
            SyntaxKind.ReturnExpr => new ReturnExprSyntax(elements),
            SyntaxKind.AssignExpr => new AssignExprSyntax(elements),
            SyntaxKind.IfExpr => new IfExprSyntax(elements),
            SyntaxKind.LoopExpr => new LoopExprSyntax(elements),
            SyntaxKind.BlockExpr => new BlockSyntax(elements),
            SyntaxKind.Arm => new ArmSyntax(elements),
            SyntaxKind.TrueLiteral => new TrueLiteralSyntax(elements),
            SyntaxKind.FalseLiteral => new FalseLiteralSyntax(elements),
            SyntaxKind.NumberLiteral => new NumberLiteralSyntax(elements),
            SyntaxKind.StringExpr => new StringExprSyntax(elements),
            SyntaxKind.ErrorExpr => new ErrorExprSyntax(elements),
            SyntaxKind.ExprStmt => new ExprStmtSyntax(elements),
            SyntaxKind.VarDecl => new VarDeclSyntax(elements),
            SyntaxKind.Param => new ParamSyntax(elements),
            SyntaxKind.FnDecl => new FnDeclSyntax(elements),
            _ => new SyntaxNode(kind, elements),
        };
}