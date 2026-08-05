namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private enum Precedence
    {
        // Ordered from lowest to highest, so the int value can be used 
        // for comparison.
            
        LogicOr,
        LogicAnd,
        LogicNot,

        Comparison,

        Sum,
        Factor,

        Negate,

        ArgList,

        Dot,
    }

    private enum PrecedenceComparison
    {
        RightBindsTighter,
        LeftBindsTighter,
        Ambiguous,
    }
    
    private static class PrecedenceTable
    {
        public static Precedence? TryGetPrefixPrecedence(TokenKind kind)
            => kind switch
            {
                TokenKind.Minus => Precedence.Negate,
                TokenKind.NotKw => Precedence.LogicNot,
                _ => null
            };

        public static Precedence? TryGetInfixPrecedence(TokenKind kind) => kind switch
        {
            TokenKind.Dot => Precedence.Dot,
            TokenKind.OpenParen => Precedence.ArgList,

            TokenKind.Star or TokenKind.Slash => Precedence.Factor,
            TokenKind.Plus or TokenKind.Minus => Precedence.Sum,

            TokenKind.LessThan or TokenKind.LessThanEqual or TokenKind.GreaterThan or TokenKind.GreaterThanEqual
                or TokenKind.DoubleEqual or TokenKind.BangEqual => Precedence.Comparison,

            TokenKind.AndKw => Precedence.LogicAnd,
            TokenKind.OrKw => Precedence.LogicOr,

            _ => null
        };

        public static PrecedenceComparison Compare(Precedence left, Precedence right)
        {
            // --- Special-case ambiguity pairs
            if (left == right && left is Precedence.Comparison)
                return PrecedenceComparison.Ambiguous;

            // --- Compare
            // If they are equal, LeftBindsTighter is returned.
            // That means, they will be left-associative.
            return (int)left < (int)right
                ? PrecedenceComparison.RightBindsTighter
                : PrecedenceComparison.LeftBindsTighter;
        }
    }
}