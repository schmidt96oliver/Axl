using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public sealed class Lexer
{
    private ref struct Scanner(SourceFileView source, DiagnosticBag diagnosticBag)
    {
        private ReadOnlySpan<char> _text = source.TextSpan;
        private int _start = 0, _next = 0;
        private ImmutableArray<Token>.Builder _tokens = ImmutableArray.CreateBuilder<Token>();

        public bool IsAtEnd
            => _next >= _text.Length;
        
        public char? Peek()
            => _next < _text.Length ? _text[_next] : null;

        public char Advance()
        {
            Debug.Assert(_next < _text.Length);
            return _text[_next++];
        }

        public void AdvanceWhile(Func<char, bool> predicate)
        {
            while (Peek() is char c && predicate(c))
                Advance();
        }

        public bool Match(char expected)
        {
            if (Peek() == expected)
            {
                Advance();
                return true;
            }

            return false;
        }

        public bool Match(ReadOnlySpan<char> expected)
        {
            if (_text.StartsWith(expected))
            {
                _next += expected.Length;
                return true;
            }

            return false;
        }


        public void AddToken(TokenKind kind)
        {
            Debug.Assert(_next > _start);
            _tokens.Add(Token.Simple(source.SpanFromTo(_start, _next), kind));
            
            _start = _next;
        }

        public void AddInvalidCharacter()
        {
            Debug.Assert(_next == _start + 1);
            
            var proof = diagnosticBag.ReportError(
                new Diagnostic.InvalidCharacters(source.LocationFromLength(_start, 1)));
            
            // Combine, if previous token was error as well
            var span = source.SpanFromLength(_start, 1);
            if (_tokens.Count > 0 && _tokens[^1].Kind is TokenKind.Error)
            {
                _tokens[^1] = Token.Error(proof,
                    SourceSpan.FromTo(_tokens[^1].Span, span));
            }
            else
                _tokens.Add(Token.Error(proof, span));

            _start = _next;
        }

        public void AddStringText(string processedText)
        {
            Debug.Assert(_next > _start);
            _tokens.Add(Token.StringText(source.SpanFromTo(_start, _next), processedText));
            _start = _next;
        }

        public void AddIdentifier()
        {
            Debug.Assert(_next > _start);
            _tokens.Add(Token.Identifier(source.SpanFromTo(_start, _next),
                Identifier.FromLexer(_text[_start.._next].ToString())));
            _start = _next;
        }

        public void AddNumberLiteral(string body, NumberLiteralSuffix suffix)
        {
            Debug.Assert(_next > _start);
            _tokens.Add(Token.NumberLiteral(source.SpanFromTo(_start, _next), body, suffix));
            _start = _next;
        }


        public ImmutableArray<Token> DrainTokens()
            => _tokens.DrainToImmutable();
    }
    
    
    public static ImmutableArray<Token> Lex(SourceFileView source, DiagnosticBag diagnosticBag)
    {
        var scanner = new Scanner(source, diagnosticBag);
        while (!scanner.IsAtEnd)
            LexSingle(ref scanner);
        return scanner.DrainTokens();
    }

    private static void LexSingle(ref Scanner scanner)
    {
        Debug.Assert(!scanner.IsAtEnd);
        switch (scanner.Advance())
        {
            case var c when char.IsWhiteSpace(c):
                scanner.AdvanceWhile(c => char.IsWhiteSpace(c));
                scanner.AddToken(TokenKind.Whitespace);
                break;
            
            case '/' when scanner.Match('/'):
                scanner.AdvanceWhile(c => c is not '\n');
                scanner.AddToken(TokenKind.Comment);
                break;
            
            default:
                scanner.AddInvalidCharacter();
                break;
        }
    }
}