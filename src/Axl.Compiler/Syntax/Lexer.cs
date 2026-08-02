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

        public ReadOnlySpan<char> CurrentText
            => _text[_start.._next];
        
        
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
            // --- Whitespace
            case var c when char.IsWhiteSpace(c):
                scanner.AdvanceWhile(c => char.IsWhiteSpace(c));
                scanner.AddToken(TokenKind.Whitespace);
                break;
            
            // --- Comment
            case '/' when scanner.Match('/'):
                scanner.AdvanceWhile(c => c is not '\n');
                scanner.AddToken(TokenKind.Comment);
                break;
            
            // --- Identifier or Keyword
            case var c when char.IsAsciiLetter(c) || c is '_':
                scanner.AdvanceWhile(c => char.IsAsciiLetterOrDigit(c) || c is '_');
                AddIdentifierOrKeyword(ref scanner);
                break;
            
            default:
                scanner.AddInvalidCharacter();
                break;
        }
    }

    private static void AddIdentifierOrKeyword(ref Scanner scanner)
    {
        var text = scanner.CurrentText;
        
        // Short-circuit
        // Shortest keyword is 2 chars (if)
        // Longest keyword is 8 chars (continue)
        if (text.Length is < 2 or > 8)
            scanner.AddIdentifier();
        
        // --- Keyword?
        var tokenKind = text switch
        {
            "and" => TokenKind.AndKw,
            "bool" => TokenKind.BoolKw,
            "break" => TokenKind.BreakKw,
            "continue" => TokenKind.ContinueKw,
            "else" => TokenKind.ElseKw,
            "f32" => TokenKind.F32Kw,
            "f64" => TokenKind.F64Kw,
            "fn" => TokenKind.FnKw,
            "false" => TokenKind.FalseKw,
            "i32" => TokenKind.I32Kw,
            "i64" => TokenKind.I64Kw,
            "if" => TokenKind.IfKw,
            "loop" => TokenKind.LoopKw,
            "module" => TokenKind.ModuleKw,
            "not" => TokenKind.NotKw,
            "native" => TokenKind.NativeKw,
            "none" => TokenKind.NoneKw,
            "or" => TokenKind.OrKw,
            "public" => TokenKind.PublicKw,
            "private" => TokenKind.PrivateKw,
            "return" => TokenKind.ReturnKw,
            "string" => TokenKind.StringKw,
            "true" => TokenKind.TrueKw,
            "var" => TokenKind.VarKw,

            _ => TokenKind.Identifier
        };
        
        if (tokenKind is TokenKind.Identifier)
            scanner.AddIdentifier();
        else
            scanner.AddToken(tokenKind);
    }
}