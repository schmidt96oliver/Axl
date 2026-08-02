using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public sealed class Lexer
{
    private ref struct Scanner(SourceFileView source, DiagnosticBag diagnosticBag)
    {
        public readonly DiagnosticBag DiagnosticBag = diagnosticBag;
        public readonly SourceFileView Source = source;
        
        private ReadOnlySpan<char> _text = source.TextSpan;
        private int _start = 0, _next = 0;
        private ImmutableArray<Token>.Builder _tokens = ImmutableArray.CreateBuilder<Token>();

        public bool IsAtEnd
            => _next >= _text.Length;

        public ReadOnlySpan<char> CurrentText
            => _text[_start.._next];

        public int StartIndex => _start;

        public int NextIndex => _next;

        
        public char Previous
        {
            get
            {
                Debug.Assert(_next > _start);
                return _text[_next - 1];
            }
        }
        
        public char Peek(int skip = 0)
        {
            Debug.Assert(skip >= 0);
            return _next + skip < _text.Length ? _text[_next + skip] : '\0';
        }

        public char Advance()
        {
            Debug.Assert(_next < _text.Length);
            return _text[_next++];
        }

        public void AdvanceWhile(Func<char, bool> predicate)
        {
            while (!IsAtEnd && predicate(Peek()))
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
            _tokens.Add(Token.Simple(Source.SpanFromTo(_start, _next), kind));
            
            _start = _next;
        }

        public void AddInvalidCharacter()
        {
            Debug.Assert(_next == _start + 1);
            
            var proof = DiagnosticBag.ReportError(
                new Diagnostic.InvalidCharacters(Source.LocationFromLength(_start, 1)));
            
            // Combine, if previous token was error as well
            var span = Source.SpanFromLength(_start, 1);
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
            _tokens.Add(Token.StringText(Source.SpanFromTo(_start, _next), processedText));
            _start = _next;
        }

        public void AddIdentifier()
        {
            Debug.Assert(_next > _start);
            _tokens.Add(Token.Identifier(Source.SpanFromTo(_start, _next),
                Identifier.FromLexer(_text[_start.._next].ToString())));
            _start = _next;
        }

        public void AddNumberLiteral(string body, NumberLiteralSuffix suffix)
        {
            Debug.Assert(_next > _start);
            _tokens.Add(Token.NumberLiteral(Source.SpanFromTo(_start, _next), body, suffix));
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
                scanner.AdvanceWhile(char.IsWhiteSpace);
                scanner.AddToken(TokenKind.Whitespace);
                break;

            // --- Comment
            case '/' when scanner.Match('/'):
                scanner.AdvanceWhile(c => c is not '\n');
                scanner.AddToken(TokenKind.Comment);
                break;

            // --- Identifier or Keyword
            case var c when char.IsAsciiLetter(c) || c is '_':
                scanner.AdvanceWhile(cc => char.IsAsciiLetterOrDigit(cc) || cc is '_');
                AddIdentifierOrKeyword(ref scanner);
                break;

            // --- Numbers
            case var c when char.IsAsciiDigit(c):
            case '.' when char.IsAsciiDigit(scanner.Peek()):
                LexNumber(ref scanner);
                break;

            // --- Symbols
            case '.':
                scanner.AddToken(TokenKind.Dot);
                break;
            case ',':
                scanner.AddToken(TokenKind.Comma);
                break;
            case ';':
                scanner.AddToken(TokenKind.Semicolon);
                break;
            case ':':
                scanner.AddToken(TokenKind.Colon);
                break;
            
            case '{':
                scanner.AddToken(TokenKind.OpenBrace);
                break;
            case '}':
                scanner.AddToken(TokenKind.CloseBrace);
                break;
            case '(':
                scanner.AddToken(TokenKind.OpenParen);
                break;
            case ')':
                scanner.AddToken(TokenKind.CloseParen);
                break;

            case '=':
                if (scanner.Match('>'))
                    scanner.AddToken(TokenKind.RightDoubleArrow);
                else if (scanner.Match('='))
                    scanner.AddToken(TokenKind.DoubleEqual);
                else
                    scanner.AddToken(TokenKind.Equal);
                break;

            case '!' when scanner.Match('='):
                scanner.AddToken(TokenKind.BangEqual);
                break;

            case '-':
                if (scanner.Match('>'))
                    scanner.AddToken(TokenKind.RightArrow);
                else if (scanner.Match('='))
                    scanner.AddToken(TokenKind.MinusEqual);
                else
                    scanner.AddToken(TokenKind.Minus);
                break;
            case '+':
                scanner.AddToken(scanner.Match('=') ? TokenKind.PlusEqual : TokenKind.Plus);
                break;
            case '*':
                scanner.AddToken(TokenKind.Star);
                break;
            case '/':
                scanner.AddToken(TokenKind.Slash);
                break;

            case '<':
                scanner.AddToken(scanner.Match('=')
                    ? TokenKind.LessThanEqual
                    : TokenKind.LessThan);
                break;

            case '>':
                scanner.AddToken(scanner.Match('=')
                    ? TokenKind.GreaterThanEqual
                    : TokenKind.GreaterThan);
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

    private static void LexNumber(ref Scanner scanner)
    {
        Debug.Assert(scanner.CurrentText.Length == 1);
        Debug.Assert(char.IsAsciiDigit(scanner.Previous) || scanner.Previous is '.');

        var bodyBuilder = new StringBuilder();
        
        // --- Digits
        var hadDot = scanner.Previous is '.';
        bodyBuilder.Append(scanner.Previous);
        while (scanner.Peek() is var c && (char.IsAsciiDigit(c) || c is '_'))
        {
            scanner.Advance();
            if (c is not '_')
                bodyBuilder.Append(c);
        }

        if (!hadDot && scanner.Peek() is '.' && char.IsAsciiDigit(scanner.Peek(1)))
        {
            bodyBuilder.Append(scanner.Advance()); // .
            bodyBuilder.Append(scanner.Advance()); // digit
            
            while (scanner.Peek() is var c && (char.IsAsciiDigit(c) || c is '_'))
            {
                scanner.Advance();
                if (c is not '_')
                    bodyBuilder.Append(c);
            }
        }

        // --- Suffix
        var suffix = NumberLiteralSuffix.None;
        if (char.IsAsciiLetter(scanner.Peek()))
        {
            // Advance an entire identifier
            var suffixStart = scanner.CurrentText.Length;
            scanner.AdvanceWhile(c => char.IsAsciiLetterOrDigit(c) || c is '_');
            
            // Parse it as a suffix
            suffix = scanner.CurrentText[suffixStart..] switch
            {
                "i32" => NumberLiteralSuffix.I32,
                "i64" => NumberLiteralSuffix.I64,
                "f32" => NumberLiteralSuffix.F32,
                "f64" => NumberLiteralSuffix.F64,
                _ => NumberLiteralSuffix.None
            };
            
            // Invalid?
            if (suffix is NumberLiteralSuffix.None)
            {
                // Suffix is invalid. Report an error and let suffix be None.
                // The invalid suffix text will be part of the token, but practically,
                // this will never be read. The body is still valid.
                
                scanner.DiagnosticBag.ReportError(new Diagnostic.UnknownNumberSuffix(
                    scanner.Source.LocationFromTo(scanner.StartIndex + suffixStart, scanner.NextIndex),
                    Actual: scanner.CurrentText[suffixStart..].ToString()));
            }
        }
        
        scanner.AddNumberLiteral(bodyBuilder.ToString(), suffix);
    }
}