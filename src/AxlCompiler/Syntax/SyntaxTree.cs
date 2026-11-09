using System.Collections.Immutable;
using System.Diagnostics;
using AxlCompiler.Diagnostics;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Text;

namespace AxlCompiler.Syntax;

public sealed class SyntaxTree
{
    /// <summary>
    /// The <see cref="SyntaxKind.File"/> node spanning the whole file.
    /// </summary>
    public FileSyntax FileSyntax { get; }
    
    public SourceText SourceText { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public bool HasError { get; }

    /// <summary>
    /// All tokens that make up this syntax tree in a flat
    /// list from left to right. Includes trivia.
    /// </summary>
    public ImmutableArray<Token> Tokens
    {
        get
        {
            if (field.IsDefault)
            {
                var tokens = ImmutableArray.CreateBuilder<Token>();
                Recurse(FileSyntax);
                
                field = tokens.DrainToImmutable();
                Debug.Assert(FileSyntax.FullRange.IsPartitionedBy(field.Select(t => t.FullRange)));

                void Recurse(SyntaxNode node)
                {
                    foreach (var child in node.Children)
                    {
                        if (child is Token token)
                            tokens.Add(token);
                        if (child is SyntaxNode childNode)
                            Recurse(childNode);
                    }
                }
            }

            return field;
        }
    }

    
    internal SyntaxTree(FileSyntax fileSyntax, SourceText sourceText, ImmutableArray<Diagnostic> diagnostics, bool hasError)
    {
        Guard.MustBe(fileSyntax.Kind is SyntaxKind.File);

        FileSyntax = fileSyntax;
        SourceText = sourceText;
        Diagnostics = diagnostics;
        HasError = hasError;
    }


    /// <summary>
    /// Returns the token immediately to the right of <paramref name="position"/>,
    /// excluding missing tokens.
    /// </summary>
    public Token TokenAt(int position)
    {
        Guard.InRange(position >= 0 && position <= SourceText.Length);
        return Tokens[TokenIndexContaining(position)];
    }

    /// <summary>
    /// Returns the non-trivia, non-empty token left of <paramref name="position"/>.
    /// Returns <c>null</c>, if no such token exists left of <paramref name="position"/>.
    /// </summary>
    public Token? TrySignificantTokenLeftOf(int position)
    {
        Guard.InRange(position >= 0 && position <= SourceText.Length);
        if (position == 0) return null;
        
        var index = TokenIndexContaining(position - 1);

        while (Tokens[index] is { Kind.IsTrivia: true } or { FullRange.Length: 0 })
        {
            index--;
            if (index < 0) return null;
        }

        return Tokens[index];
    }

    /// <summary>
    /// Returns the token directly left of <paramref name="position"/>
    /// including trivia and excluding missing tokens. Returns <c>null</c>, if
    /// <paramref name="position"/> is at the start of file.
    /// </summary>
    public Token? TryTokenLeftOf(int position)
    {
        Guard.InRange(position >= 0 && position <= SourceText.Length);
        if (position == 0) return null;
        return Tokens[TokenIndexContaining(position - 1)];
    }

    /// <summary>
    /// Returns the index of the token that contains the character at <paramref name="position"/>.
    /// EOF is seen to contain <see cref="SourceText.Length"/>. Returns only indicies where
    /// the token is EOF or non-empty.
    /// </summary>
    private int TokenIndexContaining(int position)
    {
        Guard.InRange(position >= 0 && position <= SourceText.Length);
        
        var lo = 0;
        var hi = Tokens.Length - 1;

        while (lo <= hi)
        {
            var pivot = (lo + hi) / 2;
            var token = Tokens[pivot]; 
            if (token.FullRange.Start <= position)
                lo = pivot + 1;
            else
                hi = pivot - 1;
        }

        Debug.Assert(hi >= 0 && hi < Tokens.Length);
        Debug.Assert(Tokens[hi] is { Kind: TokenKind.Eof } or { FullRange.Length: > 0 });
        return hi;
    }
}