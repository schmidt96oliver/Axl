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
        NextLineStartsBlock,
        InsideBlock
    }
    
    private readonly SourceView _source;
    private readonly DiagnosticBag _diagnosticBag;
    private readonly List<TaxlToken> _tokens = [];

    private int _start, _next;

    private Mode _mode = Mode.Taxl;
    
    private TaxlLexer(SourceView source, DiagnosticBag diagnosticBag)
    {
        _source = source;
        _diagnosticBag = diagnosticBag;

        _next = 0;
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
    }

    private void AddErrorToken(ErrorGuaranteed _, int insertIndex, int start, int end)
    {
        var span = _source.GetSpanFromTo(start, end);
        _tokens.Insert(insertIndex, new TaxlToken(span, TaxlTokenKind.Error, _source.GetText(span)));
    }

    
    private List<TaxlToken> Lex()
    {
        // We wrap lexing in a loop to collect erroneous characters
        // into a single error token.
        var text = _source.TextSpan;
        
        var loopStart = _start;
        var errorTokenPosition = _tokens.Count;
        
        Debug.Assert(_start == _next);
        while (_next < text.Length)
        {
            // --- Inside a block?
            if (_mode is Mode.InsideBlock)
            {
                LexInsideBlock(text);
                loopStart = _next;
                errorTokenPosition = _tokens.Count;
                _start = _next;
                continue;
            }
            
            // --- Lex
            if (TryLex(text))
            {
                // --- Error token before?
                if (_start > loopStart)
                    AddError();

                loopStart = _next;
                errorTokenPosition = _tokens.Count;
                _start = _next;
            }
            else
            {
                // We could not lex a token here, so its an error.
                Advance(text);
                _start = _next;
            }
            
            
        }
        

        // --- End of file
        // Add error if there was one
        if (_start > loopStart)
            AddError();

        return _tokens;
        
        
        void AddError()
        {
            var span = _source.GetLocationFromTo(loopStart, _start);
            var proof = _diagnosticBag.ReportError(new Diagnostic.InvalidCharacters(span));
            AddErrorToken(proof, insertIndex: errorTokenPosition, loopStart, _start);
        }
    }

    private bool TryLex(ReadOnlySpan<char> text)
    {
        switch (Peek(text))
        {
            // --- Newline
            case '\n':
                Advance(text);
                AddToken(TaxlTokenKind.Newline);

                if (_mode is Mode.NextLineStartsBlock)
                    _mode = Mode.InsideBlock;
                
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
                _mode = _tokens[^1].Text switch
                {
                    "#begin" or "#beginfile" => Mode.NextLineStartsBlock,
                    
                    // We need to check for end markers as well, since
                    // #begin and #end could be on the same line.
                    "#end" or "#endfile" => Mode.Taxl,
                    _ => _mode
                };
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
        // Advance as long, as we find end of endblock directive
        while (Peek(text) is char c)
        {
            if (text[_next..].StartsWith("#end") || text[_next..].StartsWith("#endfile"))
            {
                // We have found the end!
                // Now we have consumed whitespace and the last newline, which we need to back up.
                // We know, that _next is valid, so we just search going backward.
                while (_next > 0)
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

            Advance(text);
        }
        
        if (_next > _start)
            AddToken(TaxlTokenKind.AxlText);
        _mode = Mode.Taxl;
    }

    public static List<TaxlToken> Lex(SourceView source, DiagnosticBag diagnosticBag)
        => new TaxlLexer(source, diagnosticBag).Lex();
}