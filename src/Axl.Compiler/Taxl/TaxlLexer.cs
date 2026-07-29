using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Taxl;

public sealed class TaxlLexer
{
    private readonly SourceView _source;
    private readonly DiagnosticBag _diagnosticBag;

    private TaxlLexer(SourceView source, DiagnosticBag diagnosticBag)
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
    
    
    private ImmutableArray<TaxlToken> LexTaxl()
    {
        var text = _source.TextSpan;
        var textStartDirective = TextStartDirective.None;
        var tokenStart = 0;

        var tokens = ImmutableArray.CreateBuilder<TaxlToken>();
        
        while (tokenStart < text.Length)
        {
            // --- Lex single/error token
            var errorAndSingle = LexErrorAndSingle(text, tokenStart);
            tokens.Add(errorAndSingle.Item1);
            if (errorAndSingle.Item2 is TaxlToken scndToken)
                tokens.Add(scndToken);
            
            // --- Advance
            tokenStart += errorAndSingle.Item1.Span.Length + (errorAndSingle.Item2?.Span.Length ?? 0);
            
            // --- Check mode switches
            var token = tokens[^1];
            switch (token.Kind)
            {
                // --- Directive that begin a text block?
                case TaxlTokenKind.Directive when textStartDirective is TextStartDirective.None:
                    textStartDirective = GetStartDirective(token.Text);
                    break;
                
                // --- Newline that begins a text block?
                case TaxlTokenKind.Newline when textStartDirective is not TextStartDirective.None:
                    tokens.Add(LexAxlText(text, tokenStart, GetStopDirective(textStartDirective)));
                    tokenStart += tokens[^1].Span.Length;
                    textStartDirective = TextStartDirective.None;
                    break;

                // --- Start and end directive on same line?
                // Note: textStartDirective is not None, because that case is handled above.
                case TaxlTokenKind.Directive when token.Text == GetStopDirective(textStartDirective):
                {
                    // We need to insert an empty AxlText token.
                    var span = SourceSpan.EmptyBefore(token.Span);
                    var axlTextToken = TaxlToken.AxlText(span, "", []);
                    tokens.Insert(tokens.Count - 1, axlTextToken);

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
                    var errorAndSingle = LexErrorAndSingle(text, tokenStart);
                    inTextTokens.Add(errorAndSingle.Item1);
                    if (errorAndSingle.Item2 is TaxlToken scndToken)
                        inTextTokens.Add(scndToken);
                    tokenStart += errorAndSingle.Item1.Span.Length + (errorAndSingle.Item2?.Span.Length ?? 0);

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
                if (TryLexSingle(text, start + length) is not TaxlToken
                    {
                        Kind: TaxlTokenKind.Directive
                    } directiveToken)
                {
                    throw new UnreachableException();
                }

                // --- Ends the Block?
                if (directiveToken.Text != stopDirective)
                {
                    // The directive doesn't stop the block.
                    // Ignore and advance past it.
                    length += directiveToken.Span.Length;
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
                    length += directiveToken.Span.Length;
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


    private (TaxlToken, TaxlToken?) LexErrorAndSingle(ReadOnlySpan<char> text, int start)
    {
        Debug.Assert(start < text.Length);
        
        var errorStart = start;
        var errorLength = 0;

        while (errorStart + errorLength < text.Length)
        {
            var tokenStart = errorStart + errorLength;
            if (TryLexSingle(text, start: tokenStart) is TaxlToken token)
            {
                // There was an error before the token that has been added,
                // so insert it before.
                if (errorLength > 0)
                    return (MakeError(text), token);

                return (token, null);
            }

            // We did not get a token here. Advance one character
            // and start lexing the next one.
            errorLength++;
        }

        // We hit the end, maybe there was an error still left
        if (errorLength > 0)
            return (MakeError(text), null);

        throw new UnreachableException($"{nameof(text)} was non empty, so there must at least be an error.");

        TaxlToken MakeError(ReadOnlySpan<char> text)
        {
            var span = _source.SpanFromLength(errorStart, errorLength);
            return TaxlToken.Error(
                _diagnosticBag.ReportError(new Diagnostic.InvalidCharacters(_source.GetLocation(span))),
                span,
                text[errorStart..(errorStart + errorLength)].ToString());
        }
    }
    
    private TaxlToken? TryLexSingle(ReadOnlySpan<char> text, int start)
    {
        Debug.Assert(!text.IsEmpty);
        
        var length = 0;

        switch (text[start..])
        {
            // --- Newline
            case ['\n', ..]:
                length++;
                return MakeToken(text, TaxlTokenKind.Newline);

            // --- Whitespace
            case [' ' or '\t' or '\r', ..]:
                while (start + length < text.Length && text[start + length] is ' ' or '\t' or '\r')
                    length++;
                return MakeToken(text, TaxlTokenKind.Whitespace);

            // --- Comment
            case ['/', '/', ..]:
                while (start + length < text.Length && text[start + length] is not '\n')
                    length++;
                return MakeToken(text, TaxlTokenKind.Comment);

            // --- Directive
            case ['#', ..]:
                length++;
                while (start + length < text.Length &&
                       text[start + length] is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_')
                    length++;
                return MakeToken(text, TaxlTokenKind.Directive);

            // --- Identifier
            case [>= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_', ..]:
                while (start + length < text.Length && text[start + length] is
                           (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')
                           or '-' or '_'
                           or (>= '0' and <= '9'))
                {
                    length++;
                }

                return MakeToken(text, TaxlTokenKind.Identifier);

            // --- String
            case ['\"', ..]:
                length++;

                // Consume until terminated, end or newline
                while (start + length < text.Length && text[start + length] is not ('\"' or '\n'))
                    length++;

                if (start + length < text.Length && text[start + length] is '\"')
                {
                    length++;
                    return MakeToken(text, TaxlTokenKind.String);
                }

                var errorSpan = _source.SpanFromLength(start, length);
                return TaxlToken.Error(
                    _diagnosticBag.ReportError(
                        new Diagnostic.StringNotClosed(_source.GetLocation(errorSpan))),
                    errorSpan,
                    text[start..(start + length)].ToString());

        }

        return null;

        TaxlToken MakeToken(ReadOnlySpan<char> text, TaxlTokenKind kind)
            => TaxlToken.Simple(_source.SpanFromLength(start, length), kind, text[start..(start+length)].ToString());
    }
    

    public static ImmutableArray<TaxlToken> Lex(SourceView source, DiagnosticBag diagnosticBag)
    {
        var lexer = new TaxlLexer(source, diagnosticBag);
        return lexer.LexTaxl();
    }
}