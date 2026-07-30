using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Taxl;

public sealed class TaxlLexer
{
    private readonly SourceFileView _source;
    private readonly DiagnosticBag _diagnosticBag;

    private TaxlLexer(SourceFileView source, DiagnosticBag diagnosticBag)
    {
        _source = source;
        _diagnosticBag = diagnosticBag;
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


    private int RunLength(ReadOnlySpan<char> text, Func<char, bool> predicate)
    {
        var length = 0;
        while (length < text.Length && predicate(text[length]))
            length++;
        return length;
    }
    
    
    private ImmutableArray<TaxlToken> LexTaxl()
    {
        var text = _source.TextSpan;
        var textStartDirective = TextStartDirective.None;
        var tokenStart = 0;

        var tokens = ImmutableArray.CreateBuilder<TaxlToken>();
        
        while (tokenStart < text.Length)
        {
            // --- Lex single/error token
            var token = LexSingle(text, tokenStart);
            tokens.Add(token);
            
            // --- Advance
            tokenStart += token.Length;
            
            // --- Check mode switches
            switch (token.Kind)
            {
                // --- Directive that begin a text block?
                case TaxlTokenKind.Directive when textStartDirective is TextStartDirective.None:
                    textStartDirective = GetStartDirective(token.Text);
                    break;
                
                // --- Newline that begins a text block?
                case TaxlTokenKind.Newline when textStartDirective is not TextStartDirective.None:
                    var textToken = LexAxlText(text, tokenStart, GetStopDirective(textStartDirective));
                    tokens.Add(textToken);
                    tokenStart += textToken.Length;
                    
                    textStartDirective = TextStartDirective.None;
                    break;

                // --- Start and end directive on same line?
                // Note: textStartDirective is not None, because that case is handled above.
                case TaxlTokenKind.Directive when token.Text == GetStopDirective(textStartDirective):
                {
                    // We need to insert an empty AxlText token.
                    var span = SourceSpan.EmptyBefore(token.Span);
                    var emptyTextToken = TaxlToken.AxlText(span, "", []);
                    tokens.Insert(tokens.Count - 1, emptyTextToken);

                    textStartDirective = TextStartDirective.None;
                    break;
                }
            }
        }

        return tokens.DrainToImmutable();
    }

    private TaxlToken.AxlTextToken LexAxlText(ReadOnlySpan<char> text, int start, string stopDirective)
    {
        var inTextTokens = ImmutableArray.CreateBuilder<TaxlToken>();

        var length = 0;
        while (start + length < text.Length)
        {
            var c = text[start + length];

            // --- In-text directive?
            var tokenStart = start + length;
            var tokenLength = 0;
            if (text[tokenStart..] is ['/', '/', '#', ..])
            {
                // Advance //#
                tokenLength += 3;

                // --- Lex rest of the directive and add
                while (tokenStart + tokenLength < text.Length &&
                       text[tokenStart + tokenLength] is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_')
                {
                    tokenLength++;
                }

                inTextTokens.Add(TaxlToken.Simple(_source.SpanFromLength(tokenStart, tokenLength),
                    TaxlTokenKind.Directive, text[tokenStart..(tokenStart + tokenLength)].ToString()));
                tokenStart += tokenLength;
                tokenLength = 0;

                // --- Lex taxl tokens.
                // Stop on newline, then the text continues.
                while (tokenStart < text.Length)
                {
                    var errorAndSingle = LexSingle(text, tokenStart);
                    inTextTokens.Add(errorAndSingle);
                    tokenStart += errorAndSingle.Length;

                    if (inTextTokens[^1].Kind is TaxlTokenKind.Newline)
                        break;
                }

                // We have advanced quite some text, move the axl text length accordingly
                length = tokenStart - start;

                // We have matched a token, so we must not advance another character.
                continue;
            }

            // --- Skip comments
            if (text[(start + length)..] is ['/', '/', ..])
            {
                while (start + length < text.Length && text[start + length] is not '\n')
                    length++;
                continue;
            }

            // --- Skip strings
            if (text[start + length] is '\"')
            {
                length++;
                while (start + length < text.Length && text[start + length] is not ('\n' or '\"'))
                    length++;

                if (start + length < text.Length && text[start + length] is '\"')
                    length++;
                continue;
            }

            // --- Plain directive? Could be an end.
            if (c is '#')
            {
                var directiveStart = start + length;
                var directiveToken = LexSingle(text, start + length);
                Debug.Assert(directiveToken.Kind is TaxlTokenKind.Directive);

                // --- Ends the Block?
                if (directiveToken.Text != stopDirective)
                {
                    // The directive doesn't stop the block.
                    // Ignore and advance past it.
                    length += directiveToken.Length;
                    continue;
                }

                // --- End found!
                // We still need to reject the end directive, if there are non-whitespace characters
                // on the same line before. We do that, so we ignore directives in strings and comments.

                // --- Find start of the line
                var lineStart = directiveStart;
                while (lineStart > start && text[lineStart - 1] is not '\n')
                    lineStart--;

                // --- Non-whitespace before directive?
                if (text[lineStart..directiveStart].ContainsAnyExcept(' '))
                {
                    // Ignore this directive and advance past it.
                    length += directiveToken.Length;
                    continue;
                }

                // --- End is valid!
                // Back the lexer up to the line start.
                // We also need to back up one newline, if there is one.
                length = lineStart - start;
                if (lineStart > start && text[lineStart - 1] is '\n')
                    length--;

                // Emit AxlText now, then the Taxl loop will emit Newline and
                // the end directive.
                return TaxlToken.AxlText(_source.SpanFromLength(start, length),
                    text[start..(start + length)].ToString(),
                    inTextTokens.DrainToImmutable());
            }

            // --- Advance one text character
            length++;
        }

        return TaxlToken.AxlText(_source.SpanFromLength(start, length),
            text[start..(start + length)].ToString(),
            inTextTokens.DrainToImmutable());
    }
    
    private TaxlToken LexSingle(ReadOnlySpan<char> text, int start)
    {
        Debug.Assert(start < text.Length);

        if (TryLexSingle(text, start) is TaxlToken token)
            return token;

        // At the current point, no token can be lexed.
        // So we try one character later.
        var errorLength = 1;
        
        // Note that TryLexSingle just returns null, if start position of too far.
        while (TryLexSingle(text, start + errorLength) is null)
            errorLength++;

        // At this point, the next token can start, or it's end of file.
        // We discard the last token value.
        // This is wasteful, granted, but it's the easier to read pipeline :).
        var span = _source.SpanFromLength(start, errorLength);
        return TaxlToken.Error(
            _diagnosticBag.ReportError(new Diagnostic.InvalidCharacters(_source.GetLocation(span))),
            span,
            text[start..(start + errorLength)].ToString());
    }

    private TaxlToken? TryLexSingle(ReadOnlySpan<char> text, int start)
    {
        if (start >= text.Length)
            return null;
        
        var length = 0;
        var rest = text[start..];

        switch (rest)
        {
            // --- Newline
            case ['\n', ..]:
                length = 1;
                return MakeToken(rest, TaxlTokenKind.Newline);

            // --- Whitespace
            case [' ' or '\t' or '\r', ..]:
                length = RunLength(rest, static c => c is ' ' or '\t' or '\r');
                return MakeToken(rest, TaxlTokenKind.Whitespace);

            // --- Comment
            case ['/', '/', ..]:
                length = 3;
                length += RunLength(rest[3..], static c => c is not '\n');
                return MakeToken(rest, TaxlTokenKind.Comment);

            // --- Directive
            case ['#', ..]:
                length = 1;
                length += RunLength(rest[1..], static c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_');
                return MakeToken(rest, TaxlTokenKind.Directive);

            // --- Identifier
            case [>= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_', ..]:
                length = RunLength(rest, static c => c is
                    (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')
                    or '-' or '_'
                    or (>= '0' and <= '9'));
                return MakeToken(rest, TaxlTokenKind.Identifier);

            // --- String
            case ['\"', ..]:
                length = 1;
                length += RunLength(rest[1..], static c => c is not ('\"' or '\n'));
                
                if (length < rest.Length && rest[length] is '\"')
                {
                    length++;
                    return MakeToken(rest, TaxlTokenKind.String);
                }

                var errorSpan = _source.SpanFromLength(start, length);
                return TaxlToken.Error(
                    _diagnosticBag.ReportError(
                        new Diagnostic.StringNotClosed(_source.GetLocation(errorSpan))),
                    errorSpan,
                    rest[..length].ToString());
        }

        return null;

        TaxlToken MakeToken(ReadOnlySpan<char> restText, TaxlTokenKind kind)
            => TaxlToken.Simple(_source.SpanFromLength(start, length), kind, restText[..length].ToString());
    }
    

    public static ImmutableArray<TaxlToken> Lex(SourceFileView source, DiagnosticBag diagnosticBag)
    {
        var lexer = new TaxlLexer(source, diagnosticBag);
        return lexer.LexTaxl();
    }
}