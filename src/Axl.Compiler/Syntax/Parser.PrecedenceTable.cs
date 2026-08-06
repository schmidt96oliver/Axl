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

    /// <summary>
    /// The operator the current operand expression is nested under. The token is
    /// carried along, so diagnostics can point at both sides of a conflict. The
    /// precedence cannot be derived from the token alone, since prefix and infix
    /// operators share spellings.
    /// </summary>
    private readonly record struct LeftOperator(Precedence Precedence, Token Token);

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
            // --- Ambiguity pairs
            switch (left, right)
            {
                case (Precedence.Comparison, Precedence.Comparison):
                case (Precedence.LogicAnd, Precedence.LogicOr):
                case (Precedence.LogicOr, Precedence.LogicAnd):
                    return PrecedenceComparison.Ambiguous;
            }
            
            // --- Compare
            // If they are equal, LeftBindsTighter is returned.
            // That means, they will be left-associative.
            return (int)left < (int)right
                ? PrecedenceComparison.RightBindsTighter
                : PrecedenceComparison.LeftBindsTighter;
        }
    }
}