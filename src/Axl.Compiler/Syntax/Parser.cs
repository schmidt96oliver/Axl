using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private readonly SourceFileView _source;
    private readonly DiagnosticBag _diagnosticBag;

    private readonly Scanner _scanner;


    private Parser(SourceFileView source, Scanner scanner, DiagnosticBag diagnosticBag)
    {
        _source = source;
        _scanner = scanner;
        _diagnosticBag = diagnosticBag;
    }


    private bool AdvanceOrError(TokenKind expectedKind)
    {
        if (!_scanner.TryAdvance(expectedKind))
        {
            _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
                _source, _scanner.Peek(0), expectedKind));
            return false;
        }

        return true;
    }


    private void ReportUnexpected(SyntaxCategory expected)
        => _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
            _source, _scanner.Peek(0), expected));



    private void Parse()
    {
        var file = _scanner.Open();

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            ParseStmt();
        }

        _scanner.Close(file, SyntaxKind.TreeRoot);
    }

    private record BuildingNode(SyntaxKind Kind, ImmutableArray<SyntaxElement>.Builder Nodes);

    private SyntaxTree BuildTree()
    {
        Stack<BuildingNode> nodes = [];

        //TODO: Add good trivia logic here

        var allTokens = _scanner.AllTokens;
        var fullTokenIndex = 0;
        foreach (var e in _scanner.GetEvents())
        {
            switch (e.EventKind)
            {
                case ParseEventKind.Advance:
                    // Just flush all trivia here
                    while (allTokens[fullTokenIndex].Kind.IsTrivia)
                    {
                        nodes.Peek().Nodes.Add(allTokens[fullTokenIndex]);
                        fullTokenIndex++;
                    }

                    // Add the actual node
                    nodes.Peek().Nodes.Add(allTokens[fullTokenIndex]);
                    fullTokenIndex++;
                    break;

                case ParseEventKind.Open:
                    nodes.Push(new BuildingNode(e.SyntaxKind!, ImmutableArray.CreateBuilder<SyntaxElement>()));
                    break;

                case ParseEventKind.Close:
                    var builtNode = nodes.Pop();
                    if (builtNode.Kind is SyntaxKind.TreeRoot)
                    {
                        Debug.Assert(nodes.Count == 0);

                        // Flush all trivia here
                        while (fullTokenIndex < allTokens.Length)
                        {
                            builtNode.Nodes.Add(allTokens[fullTokenIndex]);
                            fullTokenIndex++;
                        }

                        return new SyntaxTree(
                            children: builtNode.Nodes.DrainToImmutable(),
                            diagnostics: _diagnosticBag.Drain(),
                            hasError: _diagnosticBag.HasError);
                    }

                    nodes.Peek().Nodes.Add(
                        new SyntaxNode(builtNode.Kind, builtNode.Nodes.DrainToImmutable()));

                    break;
            }
        }

        throw new UnreachableException();
    }

    public static SyntaxTree Parse(SourceFileView source)
    {
        var diagnosticBag = new DiagnosticBag();
        var tokens = Lexer.Lex(source, diagnosticBag);

        var scanner = new Scanner(tokens);
        var parser = new Parser(source, scanner, diagnosticBag);
        parser.Parse();
        return parser.BuildTree();
    }


    #region Parsing

    private void ParseStmt()
    {
        var markOpen = _scanner.Open();

        if (_scanner.IsAt(OperandExprFirst))
        {
            ParseOperandExpr(null);
            AdvanceOrError(TokenKind.Semicolon);
            _scanner.Close(markOpen, SyntaxKind.ExprStmt);
            return;
        }

        // Could not find a valid stmt.
        ReportUnexpected(expected: SyntaxCategory.Stmt);
        _scanner.Advance();
        _scanner.Close(markOpen, SyntaxKind.Error);
    }

    private void ParseExpr()
    {
        ParseOperandExpr(null);
    }

    #endregion

    #region Operand Expressions

    private static readonly TokenSet OperandExprFirst = TokenSet.Of(
        TokenKind.TrueKw, TokenKind.FalseKw,
        TokenKind.NumberLiteral,
        TokenKind.I32Kw, TokenKind.I64Kw, TokenKind.F32Kw, TokenKind.F64Kw, TokenKind.StringKw, TokenKind.NoneKw,
        TokenKind.Identifier,
        TokenKind.StringStart,
        TokenKind.OpenParen,
        TokenKind.Minus, TokenKind.NotKw
    );

    private void ParseOperandExpr(Precedence? leftPrecedence)
    {
        Debug.Assert(_scanner.IsAt(OperandExprFirst));

        var lhs = ParsePrimaryOperandExpr();

        //TODO: Special case `(` (that is currently a normal infix operator)

        foreach (var _ in _scanner.MustAdvanceUntilEnd())
        {
            var opToken = _scanner.Peek(0);
            var opPrecedence = PrecedenceTable.TryGetInfixPrecedence(opToken.Kind);

            if (opPrecedence is null)
                break;

            var precedenceComparison = leftPrecedence is Precedence actualLeftPrec
                ? PrecedenceTable.Compare(actualLeftPrec, opPrecedence.Value)
                : PrecedenceComparison.RightBindsTighter;

            if (precedenceComparison is PrecedenceComparison.Ambiguous)
            {
                // Report error and just resume anyway
                _diagnosticBag.ReportError(new Diagnostic.AmbiguousPrecedence(
                    _source, opToken));
            }
            else if (precedenceComparison is PrecedenceComparison.LeftBindsTighter)
                break;

            Debug.Assert(precedenceComparison is PrecedenceComparison.RightBindsTighter);

            var expr = _scanner.OpenBefore(lhs);
            _scanner.Advance();  // Advance the operator

            if (_scanner.IsAt(OperandExprFirst))
            {
                ParseOperandExpr(opPrecedence);
                lhs = _scanner.Close(expr, SyntaxKind.BinaryExpr);
            }
            else
            {
                ReportUnexpected(expected: SyntaxCategory.Expr);
                _scanner.Close(expr, SyntaxKind.BinaryExpr);
                break;
            }
        }
    }

    private MarkClose ParsePrimaryOperandExpr()
    {
        Debug.Assert(_scanner.IsAt(OperandExprFirst));

        var openMark = _scanner.Open();
        var token = _scanner.Advance();

        // --- Prefix
        if (PrecedenceTable.TryGetPrefixPrecedence(token.Kind) is Precedence prefixPrecedence)
        {
            ParseOperandExpr(prefixPrecedence);
            return _scanner.Close(openMark, SyntaxKind.UnaryExpr);
        }

        switch (token.Kind)
        {
            // --- Group
            case TokenKind.OpenParen:
                ParseExpr();
                AdvanceOrError(TokenKind.CloseParen);
                return _scanner.Close(openMark, SyntaxKind.GroupExpr);

            // --- Literals
            case TokenKind.NumberLiteral:
                return _scanner.Close(openMark, SyntaxKind.NumberLiteral);

            case TokenKind.Identifier:
                return _scanner.Close(openMark, SyntaxKind.Identifier);

            case TokenKind.I32Kw:
            case TokenKind.I64Kw:
            case TokenKind.F32Kw:
            case TokenKind.F64Kw:
            case TokenKind.NoneKw:
            case TokenKind.StringKw:
                return _scanner.Close(openMark, SyntaxKind.NativeTypeName);
        }

        throw new UnreachableException();
    }


    #endregion
}