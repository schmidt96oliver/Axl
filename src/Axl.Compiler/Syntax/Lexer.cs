using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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

        public Token LastToken => _tokens[^1];

        
        public void AddToken(TokenKind kind)
        {
            Debug.Assert(_next > _start);
            
            _tokens.Add(Token.Simple(Source.SpanFromTo(_start, _next), kind));
            _start = _next;
        }

        public void AddUnknownCharacter()
        {
            Debug.Assert(_next == _start + 1);
            
            // Note: No diagnostic reported here. The parser is bound to
            // complain about an invalid character token, so it will
            // report the appropriate diagnostic.
            
            // Combine, if previous token was error as well
            var span = Source.SpanFromLength(_start, 1);
            if (_tokens.Count > 0 && _tokens[^1].Kind is TokenKind.UnknownCharacters)
            {
                _tokens[^1] = Token.Simple(
                    SourceSpan.FromTo(_tokens[^1].Span, span),
                    TokenKind.UnknownCharacters);
            }
            else
                _tokens.Add(Token.Simple(span, TokenKind.UnknownCharacters));

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

        public void AddEof()
        {
            Debug.Assert(IsAtEnd);
            Debug.Assert(_start == _next, "Every token must have been added before Eof.");

            _tokens.Add(Token.Simple(Source.SpanFromTo(_start, _next), TokenKind.Eof));
        }


        public ImmutableArray<Token> DrainTokens()
            => _tokens.DrainToImmutable();
    }
    
    
    public static ImmutableArray<Token> Lex(SourceFileView source, DiagnosticBag diagnosticBag)
    {
        var scanner = new Scanner(source, diagnosticBag);
        while (!scanner.IsAtEnd)
            LexSingle(ref scanner);
        scanner.AddEof();
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
                scanner.AdvanceWhile(c => c is not ('\n' or '\r'));
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
            
            // --- String
            case '\"':
                LexString(ref scanner);
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
                scanner.AddUnknownCharacter();
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
        {
            scanner.AddIdentifier();
            return;
        }
        
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
            "using" => TokenKind.UsingKw,
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
        Debug.Assert(char.IsAsciiDigit(scanner.CurrentText[0]) || scanner.CurrentText is ".");

        var bodyBuilder = new StringBuilder();
        
        // --- Hex form?
        if (scanner.CurrentText is "0" && scanner.Peek() is 'x' && char.IsAsciiHexDigit(scanner.Peek(1)))
        {
            scanner.Advance();  // x
            bodyBuilder.Append(scanner.CurrentText);  // 0x
            
            AdvanceBody(ref scanner, char.IsAsciiHexDigit);
            scanner.AddNumberLiteral(bodyBuilder.ToString(), NumberLiteralSuffix.None);
            return;
        }
        
        // --- Binary form?
        if (scanner.CurrentText is "0" && scanner.Peek() is 'b' && scanner.Peek(1) is '0' or '1')
        {
            scanner.Advance();  // b
            bodyBuilder.Append(scanner.CurrentText); // 0b
            
            AdvanceBody(ref scanner, c => c is '0' or '1');
            scanner.AddNumberLiteral(bodyBuilder.ToString(), NumberLiteralSuffix.None);
            return;
        }

        // --- Digits including dot
        bodyBuilder.Append(scanner.CurrentText);    // Digit or .
        AdvanceBody(ref scanner, char.IsAsciiDigit);

        if (scanner.CurrentText[0] is not '.' && scanner.Peek() is '.' && char.IsAsciiDigit(scanner.Peek(1)))
        {
            bodyBuilder.Append(scanner.Advance()); // .
            bodyBuilder.Append(scanner.Advance()); // digit
            
            AdvanceBody(ref scanner, char.IsAsciiDigit);
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
                    scanner.Source.LocationFromTo(scanner.StartIndex + suffixStart, scanner.NextIndex)));
            }
        }
        
        scanner.AddNumberLiteral(bodyBuilder.ToString(), suffix);
        return;

        void AdvanceBody(ref Scanner scanner, Func<char, bool> isDigit)
        {
            while (scanner.Peek() is var c && (c is '_' || isDigit(c)))
            {
                scanner.Advance();
                if (c is not '_')
                    bodyBuilder.Append(c);
            }
        }
    }

    private static void LexString(ref Scanner scanner)
    {
        Debug.Assert(scanner.CurrentText is "\"");

        // --- StringStart
        scanner.AddToken(TokenKind.StringStart);

        // --- Body with text and expressions
        // Eof or Newline just end the string and the Lexer goes back into
        // normal mode. No StringEnd/StringText is created empty.
        while (scanner.Peek() is var c && c is not ('\0' or '\n' or '\r'))
        {
            switch (c)
            {
                // --- Interpolation start `{`
                case '{':
                    // This function handles { and } itself and emits those tokens.
                    // When it is finished, the scanner will be at string end or another text.
                    LexStringInterpolation(ref scanner);
                    break;
        
                // --- StringEnd `"`
                case '\"':
                    scanner.Advance();
                    scanner.AddToken(TokenKind.StringEnd);
        
                    return;
                default:
                    // This only consumes the inner text and stops
                    // at ", eof/newline, {
                    LexStringText(ref scanner);
                    break;
            }
        }
    }

    private static void LexStringText(ref Scanner scanner)
    {
        var textBuilder = new StringBuilder();
        
        // Infinite loop here, because Eof is a case inside the switch :).
        while (true)
        {
            switch (scanner.Peek())
            {
                // --- Escape
                case '\\':
                {
                    scanner.Advance(); // "\"
                    if (scanner.Peek() is '\0' or '\n' or '\r')
                    {
                        // Eof and newline will end the entire string, so we need to stop here.
                        // '\' will be discarded (not included in ProcessedText).
                        scanner.DiagnosticBag.ReportError(new Diagnostic.UnknownEscapeSequence(
                            scanner.Source.LocationFromLength(scanner.NextIndex - 1, 1)));
                        goto case '\0';
                    }

                    // Handle escape character
                    switch (scanner.Advance())
                    {
                        case 'n':
                            textBuilder.Append('\n');
                            break;
                        case 'r':
                            textBuilder.Append('\r');
                            break;
                        case 't':
                            textBuilder.Append('\t');
                            break;
                        case '{':
                            textBuilder.Append('{');
                            break;
                        case '}':
                            textBuilder.Append('}');
                            break;
                        case '\\':
                            textBuilder.Append('\\');
                            break;
                        case '\"':
                            textBuilder.Append('\"');
                            break;

                        default:
                            // Report error. The escaped sequence will not be part of the
                            // processed text.
                            scanner.DiagnosticBag.ReportError(new Diagnostic.UnknownEscapeSequence(
                                scanner.Source.LocationFromLength(scanner.NextIndex - 2, 2)));
                            break;
                    }

                    break;
                }
                
                // --- Stop
                case '\n':
                case '\r':
                case '\0':
                case '\"':
                case '{':
                    if (scanner.CurrentText.Length > 0)
                        scanner.AddStringText(textBuilder.ToString());
                    return;
                
                // --- Any character just proceeds
                default:
                    textBuilder.Append(scanner.Advance());
                    break;
            }
        }
    }
    
    private static void LexStringInterpolation(ref Scanner scanner)
    {
        Debug.Assert(scanner.CurrentText.IsEmpty);

        var braceCount = 0;
        while (!scanner.IsAtEnd)
        {
            LexSingle(ref scanner);
            switch (scanner.LastToken)
            {
                case {Kind: TokenKind.OpenBrace}:
                    braceCount++;
                    break;
                
                case {Kind: TokenKind.CloseBrace}:
                    braceCount--;

                    if (braceCount == 0)
                        return;
                    break;
            }
        }
    }
}