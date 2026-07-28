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
    private enum Mode
    {
        Taxl,
        BeginSeen,
        AddFileSeen,
        AxlFromBegin,
        AxlFromAddFile,
        AxlDirectiveInBeginText,
        AxlDirectiveInAddFileText,
    }
    
    private readonly SourceView _source;
    private readonly DiagnosticBag _diagnosticBag;
    private readonly ImmutableArray<TaxlToken>.Builder _tokens;

    private int _start, _next;

    private Mode _mode = Mode.Taxl;
    
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

    
    private void LexOutsideTaxl()
    {
        // Try Lex
        // Switch last token:
        // #begin => begin seen!
        // #end and begin seen => create Text without lexing
        // \n and begin seen => LexText
    }

    private void LexText()
    {
        // Advance characters
        // on "//#" => TryLex until newline
        // on #end => Stop (was triggered from LexTaxl)
    }

    private void LexInTextTaxl()
    {
        
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
    
    
    private void Lex()
    {
        var text = _source.TextSpan;
        
        while (_next < text.Length)
        {
            if (_mode is Mode.AxlFromAddFile or Mode.AxlFromBegin)
                LexInsideBlock(text);
            else
                LexErrorAndSingle(text);
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

                _mode = _mode switch
                {
                    Mode.BeginSeen => Mode.AxlFromBegin,
                    Mode.AddFileSeen => Mode.AxlFromAddFile,
                    _ => _mode
                };
                
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

                // Check if we started or ended a block
                switch (_tokens[^1].Text)
                {
                    case "#begin":
                        _mode = Mode.BeginSeen;
                        break;
                    case "#addfile":
                        _mode = Mode.AddFileSeen;
                        break;
                    
                    // We need to check for end markers here, since start
                    // and end directive could be on the same line.
                    case "#end" when _mode is Mode.BeginSeen:
                    case "#endfile" when _mode is Mode.AddFileSeen:
                        // We need to insert an empty AxlText token before the end directive.
                        var span = SourceSpan.EmptyAt(_tokens[^1].Span.First);
                        var axlTextToken = new TaxlToken(span, TaxlTokenKind.AxlText, "");
                        _tokens.Insert(_tokens.Count - 1, axlTextToken);
                        
                        _mode = Mode.Taxl;
                        break;
                }

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

    private void LexInsideBlock(ReadOnlySpan<char> text)
    {
        Debug.Assert(_mode is Mode.AxlFromAddFile or Mode.AxlFromBegin);
        
        // Advance as long, as we find end of endblock directive
        while (Peek(text) is char c)
        {
            // --- In-text directive?
            if (Match(text, "//#"))
            {
                
            }
            
            // --- In block directive?
            if (c is '#')
            {
                // We might have a directive here. Parse it and check
                var directiveStart = _next;
                Advance(text);
                while (Peek(text) is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_')
                    Advance(text);
                
                
                //    #end asd
                
                // --- Ends the block?
                var endsBlock = _mode is Mode.AxlFromBegin
                    ? text[directiveStart.._next] is "#end"
                    : text[directiveStart.._next] is "#endfile";
                if (endsBlock)
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
                    _mode = Mode.Taxl;
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
        _mode = Mode.Taxl;
    }

    public static ImmutableArray<TaxlToken> Lex(SourceView source, DiagnosticBag diagnosticBag)
    {
        var lexer = new TaxlLexer(source, diagnosticBag);
        lexer.Lex();
        return lexer._tokens.DrainToImmutable();
    }
}