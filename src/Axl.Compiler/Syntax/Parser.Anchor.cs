namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private readonly record struct Anchor
    {
        public static readonly Anchor Forced = new Anchor(TokenSet.Of(TokenKind.Eof));
        
        
        private readonly TokenSet _set;

        private Anchor(TokenSet set)
        {
            _set = set;
        }


        public static Anchor From(TokenSet tokenSet)
            => Forced | tokenSet;

        public static Anchor Of(params ReadOnlySpan<TokenKind> kinds)
            => Forced | TokenSet.Of(kinds);
        
        
        public static Anchor operator |(Anchor a, TokenSet b) => new Anchor(a._set | b);

        public static Anchor operator |(Anchor a, TokenKind b) => new Anchor(a._set | b);
        
        public static Anchor operator |(Anchor a, Anchor b) => new Anchor(a._set | b._set);


        public bool Contains(TokenKind kind)
            => _set.Contains(kind);

        public override string ToString()
            => $"Anchor {_set}";
    }
}