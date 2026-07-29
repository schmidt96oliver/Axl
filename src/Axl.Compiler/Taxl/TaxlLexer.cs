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
        while (Peek(text) is char c)
        {
            // --- In-text directive?
            if (Match(text, "//#"))
            {
                
            }
            
            // --- Plain directive? Could be an end.
            if (c is '#')
            {
                var directiveStart = _next;
                
                // Advance the entire directive
                Advance(text);
                while (Peek(text) is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_')
                    Advance(text);
                
                // Check if it ends the block
                if (text[directiveStart.._next].SequenceEqual(stopDirective))
                {
                    // We have found the end!
                    // Now we have consumed whitespace and the last newline, which we need to back up.
                    // We know, that _next is valid, so we just search going backward.
                    
                    // Back up before the start, so that the directive goes through normal lexing.
                    _next = directiveStart;
                    while (_next > _start)
                    {
                        if (text[_next - 1] is ' ')
                            _next--;
                        else if (text[_next - 1] is '\n')
                        {
                            // Back up and then stop, because we only want the last newline.
                            _next--;
                            break;
                        }
                        else
                            break;
                    }
                
                    AddToken(TaxlTokenKind.AxlText);
                    return;
                }
                
                // Otherwise, we have advanced everything and discard it.
                // That will attach it to the AxlBlock token
                continue;
            }

            
            Advance(text);
        }
        
        if (_next > _start)
            AddToken(TaxlTokenKind.AxlText);
    }
    
    
    private void LexErrorAndSingle(ReadOnlySpan<char> text)
    {
        // Lexes single token and possible error before.

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