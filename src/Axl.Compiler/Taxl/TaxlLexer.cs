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

        // Still in text mode?
        if (textStartDirective is not TextStartDirective.None)
        {
            // That means, there was no newline after #begin and the file ended.
            // So we produce an empty AxlText token.
            tokens.Add(TaxlToken.AxlText(SourceSpan.EmptyAfter(tokens[^1].Span), "", []));
        }
        
        return tokens.DrainToImmutable();
    }

    private TaxlToken.AxlTextToken LexAxlText(ReadOnlySpan<char> text, int start, string stopDirective)
    {
        var inTextTokens = ImmutableArray.CreateBuilder<TaxlToken>();

        var length = 0;
        while (start + length < text.Length)
        {
            var current = start + length;
            switch (text[current..])
            {
                // --- In-text directive?
                case ['/', '/', '#', ..]:
                    length += LexInTextTokens(text, current, inTextTokens);
                    continue;
                
                // --- Skip comments
                case ['/', '/', ..]:
                {
                    length += RunLength(text[current..], static c => c is not '\n');
                    continue;
                }
                
                // --- Skip strings
                case ['\"', ..]:
                {
                    length++;
                    length += RunLength(text[(current + 1)..], static c => c is not ('\n' or '\"'));

                    if (start + length < text.Length && text[start + length] is '\"')
                        length++;
                    continue;
                }

                // --- #end or #endfile directive?
                case ['#', ..] when LexSingle(text, current).Text == stopDirective:
                {
                    // We still need to reject the end directive, if there are non-whitespace characters
                    // on the same line before. We do that, so we ignore directives in strings and comments.
                    // If \n is not found, lineStart == start, which is correct.
                    var lineStart = text[start..current].LastIndexOf('\n') + 1 + start;
                    if (text[lineStart..current].ContainsAnyExcept(' '))
                    {
                        // Skip the # character and continue the loop
                        length++;
                        continue;
                    }
                    
                    // End is valid. The text token runs until, but excluding, the last newline.
                    var textEnd = lineStart;
                    if (textEnd > start)
                    {
                        Debug.Assert(text[textEnd - 1] is '\n');
                        textEnd--;
                    }

                    // Emit AxlText now, then the Taxl loop will emit Newline and
                    // the end directive.
                    return TaxlToken.AxlText(_source.SpanFromTo(start, textEnd),
                        text[start..textEnd].ToString(),
                        inTextTokens.DrainToImmutable());
                }
                
                // --- Otherwise just consume one character
                default:
                    length++;
                    break;
            }
        }

        return TaxlToken.AxlText(_source.SpanFromLength(start, length),
            text[start..(start + length)].ToString(),
            inTextTokens.DrainToImmutable());
    }

    /// <summary>
    /// Returns the length that has been advanced.
    /// </summary>
    private int LexInTextTokens(ReadOnlySpan<char> text, int start, ImmutableArray<TaxlToken>.Builder tokens)
    {
        Debug.Assert(text[start..].StartsWith("//#"));

        // --- Lex //# directive
        // We cannot run that through LexSingle, because this will not consume
        // the "//" in front of "#". But we want it in the token for correct syntax
        // highlighting in the LSP.
        var directiveLength = 3 + RunLength(text[(start + 3)..],
            static c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-' or '_');
        tokens.Add(TaxlToken.Simple(_source.SpanFromLength(start, directiveLength),
            TaxlTokenKind.Directive, text[start..(start + directiveLength)].ToString()));
        
        // --- Further tokens until newline
        var tokenStart = start + directiveLength;
        while (tokenStart < text.Length)
        {
            var token = LexSingle(text, tokenStart);
            tokens.Add(token);
            tokenStart += token.Length;
            
            if (token.Kind is TaxlTokenKind.Newline)
                break;
        }

        // Return the length that has been advanced
        return tokenStart - start;
    }

    
    private bool CanStartToken(ReadOnlySpan<char> text, int start)
        => text[start..] is ['\n', ..] or [' ' or '\t' or '\r', ..] or ['/', '/', ..]
            or ['#', ..] or [>= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_', ..] or ['\"', ..];
    
    private TaxlToken LexSingle(ReadOnlySpan<char> text, int start)
    {
        Debug.Assert(start < text.Length);
        
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
                length = 2;
                length += RunLength(rest[2..], static c => c is not '\n');
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
                
                // String terminated?
                if (length < rest.Length && rest[length] is '\"')
                {
                    length++;
                }
                else
                {
                    // String has not been terminated. Report error and still add the string token.
                    _diagnosticBag.ReportError(
                        new Diagnostic.StringNotClosed(_source.LocationFromLength(start, length)));
                }

                return MakeToken(rest, TaxlTokenKind.String);
            
            // --- Error!
            default:
                Debug.Assert(!CanStartToken(text, start));
                length = 1;
                while (length < rest.Length && !CanStartToken(rest, length))
                    length++;
                
                var span = _source.SpanFromLength(start, length);
                return TaxlToken.Error(
                    _diagnosticBag.ReportError(new Diagnostic.InvalidCharacters(_source.GetLocation(span))),
                    span,
                    text[start..(start + length)].ToString());
        }

        TaxlToken MakeToken(ReadOnlySpan<char> restText, TaxlTokenKind kind)
            => TaxlToken.Simple(_source.SpanFromLength(start, length), kind, restText[..length].ToString());
    }
    

    public static ImmutableArray<TaxlToken> Lex(SourceFileView source, DiagnosticBag diagnosticBag)
    {
        var lexer = new TaxlLexer(source, diagnosticBag);
        var tokens = lexer.LexTaxl();
        Debug.Assert(source.Span.IsPartitionedBy(tokens.Select(t => t.Span)));
        return tokens;
    }
}