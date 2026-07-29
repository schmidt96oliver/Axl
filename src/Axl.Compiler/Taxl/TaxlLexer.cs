using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Taxl;

// Lexes a *.taxl file into a token stream. Tokens are already context dependent,
// i.e. anything before a directive is an error. Tokens are continuous. Inside
// an AxlText token, there can be more DirectiveTokens, which can come after the text
// token.
public sealed class TaxlLexer
{
    private readonly SourceView _source;
    private readonly DiagnosticBag _diagnosticBag;
    private readonly ImmutableArray<TaxlToken>.Builder _tokens;

    private int _start, _next;

    private TaxlLexer(SourceView source, DiagnosticBag diagnosticBag)
    {
        _source = source;
        _diagnosticBag = diagnosticBag;

        _next = 0;
        _tokens = ImmutableArray.CreateBuilder<TaxlToken>();
    }

    
    private char? Advance(ReadOnlySpan<char> text)
    {
        return _next < text.Length
            ? text[_next++]
            : null;
    }
    
    private char? Peek(ReadOnlySpan<char> text)
    {
        return _next < text.Length
            ? text[_next]
            : null;
    }

    private bool Match(ReadOnlySpan<char> text, params ReadOnlySpan<char> expected)
    {
        if (Peek(text) is char c && expected.Contains(c))
        {
            Advance(text);
            return true;
        }

        return false;
    }

    private bool Match(ReadOnlySpan<char> text, string expectedText)
    {
        if (_next + expectedText.Length >= text.Length)
            return false;

        if (text[_next..].StartsWith(expectedText))
        {
            _next += expectedText.Length;
            return true;
        }

        return false;
    }

    
    private void AddToken(TaxlTokenKind kind)
    {
        Debug.Assert(kind is not TaxlTokenKind.Error, "Use AddErrorToken instead!");
        
        var span = _source.GetSpanFromTo(_start, _next);
        _tokens.Add(new TaxlToken(span, kind, _source.GetText(span)));

        _start = _next;
    }

    private void AddErrorToken(ErrorGuaranteed _, int insertIndex, int start, int end)
    {
        var span = _source.GetSpanFromTo(start, end);
        _tokens.Insert(insertIndex, new TaxlToken(span, TaxlTokenKind.Error, _source.GetText(span)));
    }


    private enum TextStartDirective
    {
        None,
        Begin,
        Addfile
    }

    private TextStartDirective GetStartDirective(string directiveText) => directiveText switch
    {
        "#begin" => TextStartDirective.Begin,
        "#addfile" => TextStartDirective.Addfile,
        _ => TextStartDirective.None
    };

    private string GetStopDirective(TextStartDirective startDirective) => startDirective switch
    {
        TextStartDirective.Begin => "#end",
        TextStartDirective.Addfile => "#endfile",
        _ => throw new ArgumentException($"Given {nameof(startDirective)} has no corresponding end directive.",
            nameof(startDirective))
    };
    
    
    private void LexTaxl()
    {
        var text = _source.TextSpan;
        var textStartDirective = TextStartDirective.None;
        
        while (_next < text.Length)
        {
            // --- Lex single token
            LexErrorAndSingle(text);
            
            // --- Check mode switches
            var token = _tokens[^1];
            switch (token.Kind)
            {
                // --- Directive that begin a text block?
                case TaxlTokenKind.Directive when textStartDirective is TextStartDirective.None:
                    textStartDirective = GetStartDirective(token.Text);
                    break;
                
                // --- Newline that begins a text block?
                case TaxlTokenKind.Newline when textStartDirective is not TextStartDirective.None:
                    LexAxlText(text, GetStopDirective(textStartDirective));
                    textStartDirective = TextStartDirective.None;
                    break;

                // --- Start and end directive on same line?
                // Note: textStartDirective is not None, because that case is handled above.
                case TaxlTokenKind.Directive when token.Text == GetStopDirective(textStartDirective):
                {
                    // We need to insert an empty AxlText token.
                    var span = SourceSpan.EmptyAt(token.Span.First);
                    var axlTextToken = new TaxlToken(span, TaxlTokenKind.AxlText, "");
                    _tokens.Insert(_tokens.Count - 1, axlTextToken);

                    textStartDirective = TextStartDirective.None;
                    break;
                }
            }
        }
    }
    
    private void LexAxlText(ReadOnlySpan<char> text, string stopDirective)
    {
        var textStart = _start;
        
        while (Peek(text) is char c)
        {
            // --- In-text directive?
            if (Match(text, "//#"))
            {
                _start = _next - 3; // Set start to //#
                
                // --- Lex directive
                while (Peek(text) is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_')
                    Advance(text);
                AddToken(TaxlTokenKind.InTextDirective);
                
                // --- Lex taxl tokens.
                // Stop on newline, then the text continues.
                while (Peek(text) is not null)
                {
                    LexErrorAndSingle(text);
                    if (_tokens[^1].Kind is TaxlTokenKind.Newline)
                        break;
                }

                // Reset token start to axl text start
                _start = textStart;
                
                // We have matched a token, so we must not advance another character.
                continue;
            }

            // --- Skip comments
            if (Match(text, "//"))
            {
                while (Peek(text) is not (null or '\n'))
                    Advance(text);
                continue;
            }

            // --- Skip strings
            if (Match(text, '\"'))
            {
                while (Peek(text) is not (null or '\n' or '\"'))
                    Advance(text);
                Match(text, '\"');
                continue;
            }
            
            // --- Plain directive? Could be an end.
            if (c is '#')
            {
                var directiveStart = _next;

                // --- Advance entire directive
                Advance(text);
                while (Peek(text) is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_')
                    Advance(text);

                // --- Ends the Block?
                if (!text[directiveStart.._next].SequenceEqual(stopDirective))
                {
                    // The directive doesn't stop the block, so we add it will
                    // be added to the AxlText token later.
                    continue;
                }

                // --- End found!
                // We still need to reject the end directive, if there are non-whitespace characters
                // on the same line before. We do that, so we ignore directives in strings and comments.
                
                // --- Find start of the line
                var lineStart = directiveStart;
                while (lineStart > _start && text[lineStart - 1] is not '\n')
                {
                    lineStart--;
                }

                // --- Non-whitespace before directive?
                if (text[lineStart..directiveStart].ContainsAnyExcept(' '))
                {
                    // Ignore this directive. It is already advanced, so we can skip
                    // the advance step and continue with the next iteration.
                    continue;
                }

                // --- End is valid!
                // Back the lexer up to the line start.
                // We also need to back up one newline, if there is one.
                _next = lineStart;
                if (_next - 1 >= _start && text[_next - 1] is '\n')
                    _next--;
                
                // Emit AxlText now, then the Taxl loop will emit Newline and
                // the end directive.
                AddToken(TaxlTokenKind.AxlText);
                return;
            }

            // --- Advance one text character
            Advance(text);
        }
        
        if (_next > _start)
            AddToken(TaxlTokenKind.AxlText);
    }
    
    
    private void LexErrorAndSingle(ReadOnlySpan<char> text)
    {
        var errorStart = _next;
        while (_next < text.Length)
        {
            var errorEnd = _next;

            if (!TryLexSingle(text))
            {
                // We did not get a token here. Advance one character
                // and start lexing the next one.
                Advance(text);
                _start = _next;
            }
            else
            {
                // There was an error before the token that has been added,
                // so insert it before.
                if (errorEnd > errorStart)
                {
                    var span = _source.GetLocationFromTo(errorStart, errorEnd);
                    var proof = _diagnosticBag.ReportError(new Diagnostic.InvalidCharacters(span));
                    AddErrorToken(proof, insertIndex: _tokens.Count - 1, errorStart, errorEnd);
                }

                return;
            }
        }
        
        // We hit the end, maybe there was an error still left
        if (_next > errorStart)
        {
            var span = _source.GetLocationFromTo(errorStart, _next);
            var proof = _diagnosticBag.ReportError(new Diagnostic.InvalidCharacters(span));
            AddErrorToken(proof, insertIndex: _tokens.Count - 1, errorStart, _next);
            _start = _next;
        }
    }
    
    private bool TryLexSingle(ReadOnlySpan<char> text)
    {
        switch (Peek(text))
        {
            // --- Newline
            case '\n':
                Advance(text);
                AddToken(TaxlTokenKind.Newline);

                return true;
            
            // --- Whitespace
            case ' ' or '\t' or '\r':
                while (Match(text, ' ', '\t', '\r'))
                { }
                AddToken(TaxlTokenKind.Whitespace);
                return true;
            
            // --- Comment
            case '/' when Match(text, "//"):
                while (Peek(text) is not (null or '\n'))
                    Advance(text);
                AddToken(TaxlTokenKind.Comment);
                return true;
            
            // --- Directive
            case '#':
                Advance(text);
                while (Peek(text) is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_')
                    Advance(text);
                AddToken(TaxlTokenKind.Directive);

                return true;
            
            // --- Identifier
            case >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_':
                while (Peek(text) is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')
                       or '-' or '_'
                       or (>= '0' and <= '9'))
                {
                    Advance(text);
                }
                AddToken(TaxlTokenKind.Identifier);
                return true;
            
            // --- String
            case '\"':
                Advance(text);
                
                // Consume until terminated, end or newline
                while (Peek(text) is not (null or '\"' or '\n'))
                    Advance(text);
        
                if (Match(text, '\"'))
                    AddToken(TaxlTokenKind.String);
                else
                {
                    AddErrorToken(_diagnosticBag.ReportError(new Diagnostic.StringNotClosed(_source.GetLocationFromTo(_start, _next))),
                        insertIndex: _tokens.Count,
                        _start, _next);
                }

                return true;
        }

        return false;
    }
    

    public static ImmutableArray<TaxlToken> Lex(SourceView source, DiagnosticBag diagnosticBag)
    {
        var lexer = new TaxlLexer(source, diagnosticBag);
        lexer.LexTaxl();
        return lexer._tokens.DrainToImmutable();
    }
}