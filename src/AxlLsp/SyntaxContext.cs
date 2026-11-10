using AxlCompiler;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Text;
using AxlLsp.Projects;

namespace AxlLsp;

public sealed class SyntaxContext
{
    public Compilation Compilation { get; }

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

    public TokenSet Continuations
    {
        get
        {
            if (field.IsEmpty)
                field = Parser.ContinuationsAt(SyntaxTree.SourceText, Caret);

            return field;
        }
    }
    

    private SyntaxContext(int caret, SyntaxTree syntaxTree, Compilation compilation)
    {
        SyntaxTree = syntaxTree;
        Compilation = compilation;
        Caret = caret;
    }
    
    public static SyntaxContext At(int caret, SyntaxTree syntaxTree, Compilation compilation)
    {
        Guard.MustBe(compilation.SyntaxTrees.Contains(syntaxTree));
        Guard.MustBe(caret >= 0 && caret <= syntaxTree.SourceText.Length);
        return new SyntaxContext(caret, syntaxTree, compilation);
    }

    public static SyntaxContext At(SourceLocation location, Compilation compilation)
        => At(location.Start, compilation.SyntaxTreeFor(location.SourceText), compilation);
    
    public static SyntaxContext At(ProjectLocation location)
        => At(location.Location, location.Compilation);
}