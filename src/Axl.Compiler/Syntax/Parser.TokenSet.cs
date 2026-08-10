using System.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private readonly struct TokenSet
    {
        private readonly ulong _lo, _hi;
        private static readonly TokenKind[] AllKinds = Enum.GetValues<TokenKind>();

        public static TokenSet Empty => default;


        private TokenSet(ulong lo, ulong hi)
        {
            _lo = lo;
            _hi = hi;
        }

        static TokenSet()
        {
            Debug.Assert(Enum.GetValues<TokenKind>().Length <= 128,
                "Too many TokenKinds. Expand the TokenSet to accomodate.");
        }


        public static TokenSet Of(params ReadOnlySpan<TokenKind> kinds)
        {
            ulong lo = 0, hi = 0;
            foreach (var k in kinds)
            {
                var index = (int)k;
                Debug.Assert(index < 128);

                if (index < 64)
                    lo |= 1UL << index;
                else
                    hi |= 1UL << (index - 64);
            }

            return new TokenSet(lo, hi);
        }


        public static TokenSet operator |(TokenSet a, TokenSet b) => new(a._lo | b._lo, a._hi | b._hi);

        public static TokenSet operator |(TokenSet a, TokenKind b) => a | Of(b);


        public bool Contains(TokenKind kind)
        {
            var index = (int)kind;
            Debug.Assert(index < 128);

            var word = index < 64 ? _lo : _hi;
            return (word >> (index & 63) & 1) != 0;
        }

        public IEnumerable<TokenKind> GetKinds()
        {
            var thisSet = this;
            return AllKinds.Where(thisSet.Contains);
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", GetKinds())}]";
        }
    }
}