namespace AxlCompiler.Syntax;

public partial class Parser
{
    private readonly record struct Anchor
    {
        public static readonly Anchor Forced = new(TokenSet.Of(TokenKind.Eof));


        public TokenSet Set { get; }

        private Anchor(TokenSet set)
        {
            Set = set;
        }


        public static Anchor From(TokenSet tokenSet)
            => Forced | tokenSet;
        
        
        public static Anchor operator |(Anchor a, TokenSet b) => new(a.Set | b);

        public static Anchor operator |(Anchor a, TokenKind b) => new(a.Set | b);
        
        public static Anchor operator |(Anchor a, Anchor b) => new(a.Set | b.Set);


        public bool Contains(TokenKind kind)
            => Set.Contains(kind);

        public override string ToString()
            => $"Anchor {Set}";
    }
}