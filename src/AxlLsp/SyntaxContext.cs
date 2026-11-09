using AxlCompiler;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlLsp.Projects;

namespace AxlLsp;

public sealed class SyntaxContext
{
    public SyntaxTree SyntaxTree { get; }

    /// <summary>
    /// The caret sits left of the character at <see cref="Caret"/>.
    /// </summary>
    public int Caret { get; }

    private bool _hasLeftToken = false;

    public bool IsInsideString
        => LeftToken?.Kind is TokenKind.StringStart or TokenKind.StringText
           || LeftToken is { Kind: TokenKind.CloseBrace, Parent: StringInterpolationSyntax };

    public bool IsInsideComment
        => LeftToken?.Kind is TokenKind.Comment;
    
    private Token? LeftToken
    {
        get
        {
            if (!_hasLeftToken)
            {
                field = SyntaxTree.TryTokenLeftOf(Caret);
                _hasLeftToken = true;
            }

            return field;
        }
    }
    
    private bool _hasTouchingWord = false;
    public Token? TouchingWord
    {
        get
        {
            if (!_hasTouchingWord)
            {
                // let a = Type(|Field = 1)
                // let b = x.|Foo
                // let c = foo|(1)
                if (LeftToken is not null && SyntaxFacts.IsWord(LeftToken.Kind))
                    field = LeftToken;
                else
                {
                    var tokenAt = SyntaxTree.TokenAt(Caret);
                    field = SyntaxFacts.IsWord(tokenAt.Kind) ? tokenAt : null;
                }
                
                _hasTouchingWord = true;
            }

            return field;
        }
    }

    private bool _hasTargetToken = false;
    public Token? TargetToken
    {
        get
        {
            if (!_hasTargetToken)
            {
                field = SyntaxTree.TrySignificantTokenLeftOf(TouchingWord?.Range?.Start ?? Caret);
                _hasTargetToken = true;
            }

            return field;
        }
    }



    private SyntaxContext(int caret, SyntaxTree syntaxTree)
    {
        SyntaxTree = syntaxTree;
        Caret = caret;
    }
    
    public static SyntaxContext At(int caret, SyntaxTree syntaxTree)
    {
        Guard.MustBe(caret >= 0 && caret <= syntaxTree.SourceText.Length);
        return new SyntaxContext(caret, syntaxTree);
    }

    public static SyntaxContext At(ProjectLocation location)
        => At(location.Location.Start, location.Compilation.SyntaxTreeFor(location.Location.SourceText));
}