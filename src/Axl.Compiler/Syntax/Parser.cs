using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public class Parser
{
    private enum ParseEventKind
    {
        Open,
        Close,
        Advance,
    }

    /// <param name="SyntaxKind">
    /// Only meaningful on <see cref="ParseEventKind.Open"/>.
    /// <c>null</c> if no kind has been assigned yet.
    /// </param>
    private readonly record struct ParseEvent(ParseEventKind EventKind, SyntaxKind SyntaxKind = SyntaxKind.Error);

    private readonly record struct MarkOpen(int OpenIndex);

    private readonly record struct MarkClose(int OpenIndex);

    internal readonly struct TokenSet
    {
        private readonly ulong _lo, _hi;

        public static TokenSet Empty => default;

        private TokenSet(ulong lo, ulong hi)
        {
            _lo = lo;
            _hi = hi;
        }
        
        public static TokenSet Of(params ReadOnlySpan<TokenKind> kinds)
        {
            ulong lo = 0, hi = 0;
            foreach (var k in kinds)
            {
                var index = (int)k;
                Debug.Assert(index < 128);
                
                if (index < 64) 
                    lo |= 1UL << index;
                else        
                    hi |= 1UL << (index - 64);
            }
            return new TokenSet(lo, hi);
        }

        
        public static TokenSet operator |(TokenSet a, TokenSet b) => new(a._lo | b._lo, a._hi | b._hi);
        
        public static TokenSet operator |(TokenSet a, TokenKind b) => a | Of(b);
        
        
        public bool Contains(TokenKind kind)
        {
            var index = (int)kind;
            Debug.Assert(index < 128);
            
            var word = index < 64 ? _lo : _hi;
            return (word >> (index & 63) & 1) != 0;
        }
    }
    
    
    private readonly SourceFileView _source;
    private readonly ImmutableArray<Token> _allTokens;
    private readonly DiagnosticBag _diagnosticBag;
    
    private readonly ImmutableArray<Token> _tokens;
    private int _nextToken;

    private List<ParseEvent> _events;

    
    private bool IsAtEnd => IsAt(TokenKind.Eof);
    
    
    private Parser(SourceFileView source, ImmutableArray<Token> allTokens, DiagnosticBag diagnosticBag)
    {
        _source = source;
        _allTokens = allTokens;
        _tokens = [.. allTokens.Where(token => !token.Kind.IsTrivia)];
        _diagnosticBag = diagnosticBag;

        _events = [];
        _nextToken = 0;
    }


    private MarkOpen Open()
    {
        _events.Add(new ParseEvent(ParseEventKind.Open));
        return new MarkOpen(_events.Count - 1);
    }

    private MarkClose Close(MarkOpen openMark, SyntaxKind kind)
    {
        _events[openMark.OpenIndex] = new ParseEvent(ParseEventKind.Open, kind);
        _events.Add(new ParseEvent(ParseEventKind.Close));
        return new MarkClose(openMark.OpenIndex);
    }

    /// <summary>
    /// Requires a <see cref="MarkClose"/>, because the mark will be invalidated!
    /// </summary>
    private MarkOpen OpenBefore(MarkClose before)
    {
        _events.Insert(before.OpenIndex, new ParseEvent(ParseEventKind.Open));
        return new MarkOpen(before.OpenIndex);
    }

    private Token Advance()
    {
        Debug.Assert(!IsAtEnd);
        
        _events.Add(new ParseEvent(ParseEventKind.Advance));
        return _tokens[_nextToken++];
    }

    private Token Peek(int lookahead = 1)
    { 
        Debug.Assert(lookahead >= 0);
        if (_nextToken + lookahead < _tokens.Length)
            return _tokens[_nextToken + lookahead];
        
        Debug.Assert(_tokens[^1].Kind is TokenKind.Eof);
        return _tokens[^1];
    }
    
    
    private bool IsAt(TokenKind kind)
        => Peek(0).Kind == kind;

    private bool IsAt(TokenSet set)
        => set.Contains(Peek(0).Kind);

    private bool TryAdvance(TokenKind expectedKind)
    {
        if (IsAt(expectedKind))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool AdvanceOrError(TokenKind expectedKind)
    {
        if (!TryAdvance(expectedKind))
        {
            _diagnosticBag.ReportError(new Diagnostic.UnexpectedToken(
                _source, Peek(0), expectedKind));
            return false;
        }

        return true;
    }

    private void AdvanceKnown(TokenKind knownKind)
    {
        Debug.Assert(IsAt(knownKind));
        Advance();
    }
    
    
    
    private void Parse()
    {
        var file = Open();

        while (!IsAtEnd)
        {
            ParseStmt();
        }
        
        Close(file, SyntaxKind.TreeRoot);
    }

    private record BuildingNode(SyntaxKind Kind, ImmutableArray<SyntaxElement>.Builder Nodes);
    
    private SyntaxTree BuildTree()
    {
        Stack<BuildingNode> nodes = [];
        
        //TODO: Add good trivia logic here
        
        var fullTokenIndex = 0;
        foreach (var e in _events)
        {
            switch (e.EventKind)
            {
                case ParseEventKind.Advance:
                    // Just flush all trivia here
                    while (_allTokens[fullTokenIndex].Kind.IsTrivia)
                    {
                        nodes.Peek().Nodes.Add(_allTokens[fullTokenIndex]);
                        fullTokenIndex++;
                    }
                    
                    // Add the actual node
                    nodes.Peek().Nodes.Add(_allTokens[fullTokenIndex]);
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
                        while (fullTokenIndex < _allTokens.Length)
                        {
                            builtNode.Nodes.Add(_allTokens[fullTokenIndex]);
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

        var parser = new Parser(source, tokens, diagnosticBag);
        parser.Parse();
        return parser.BuildTree();
    }
    
    
    #region Parsing

    private void ParseStmt()
    {
        var markOpen = Open();
        
        if (IsAt(OperandExprFirst))
        {
            ParseOperandExpr();
            AdvanceOrError(TokenKind.Semicolon);
            Close(markOpen, SyntaxKind.ExprStmt);
            return;
        }
        
        // Could not find a valid stmt.
        var actualToken = Advance();
        _diagnosticBag.ReportError(new Diagnostic.ExpectedStmt(_source, actualToken));
        Close(markOpen, SyntaxKind.Error);
    }
    
    #endregion

    #region Operand Expressions

    private static readonly TokenSet OperandExprFirst = TokenSet.Of(
        TokenKind.TrueKw, TokenKind.FalseKw,
        TokenKind.NumberLiteral,
        TokenKind.I32Kw, TokenKind.I64Kw, TokenKind.F32Kw, TokenKind.F64Kw, TokenKind.StringKw, TokenKind.NoneKw,
        TokenKind.Identifier,
        TokenKind.StringStart,
        TokenKind.OpenParen
    );
    
    private void ParseOperandExpr()
    {
        Debug.Assert(IsAt(OperandExprFirst));

        var lhs = ParsePrimaryOperandExpr();

        // [[[1] + [2]] - [4]]
        
        while (Peek(0).Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var expr = OpenBefore(lhs);
            Advance();  // Advance the operator


            if (!IsAt(OperandExprFirst))
            {
                _diagnosticBag.ReportError(new Diagnostic.ExpectedExpr(
                    _source, Peek(0)));
                Close(expr, SyntaxKind.Error);
                break;
            }

            
            ParseOperandExpr();
            lhs = Close(expr, SyntaxKind.BinaryExpr);
        }
    }

    private MarkClose ParsePrimaryOperandExpr()
    {
        var openMark = Open();
        var syntaxKind = Advance().Kind switch
        {
            TokenKind.NumberLiteral => SyntaxKind.NumberLiteral,
            TokenKind.Identifier => SyntaxKind.Identifier,
            _ => SyntaxKind.Error
        };
        return Close(openMark, syntaxKind);
    }
    

    #endregion
}