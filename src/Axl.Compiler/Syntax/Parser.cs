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
    private readonly record struct ParseEvent(ParseEventKind EventKind, SyntaxKind? SyntaxKind = null);

    private readonly record struct MarkOpen(int OpenIndex);

    private readonly record struct MarkClose(int OpenIndex);
    
    
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

    private void Advance()
    {
        Debug.Assert(!IsAtEnd);
        
        _events.Add(new ParseEvent(ParseEventKind.Advance));
        _nextToken++;
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
        => Peek(0)?.Kind == kind;

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
        
    }

    private SyntaxTree BuildTree()
    {
        return new SyntaxTree([], [], false);
    }

    public static SyntaxTree Parse(SourceFileView source)
    {
        var diagnosticBag = new DiagnosticBag();
        var tokens = Lexer.Lex(source, diagnosticBag);

        var parser = new Parser(source, tokens, diagnosticBag);
        parser.Parse();
        return parser.BuildTree();
    }
}