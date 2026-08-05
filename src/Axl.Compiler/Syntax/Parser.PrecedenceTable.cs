namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private static class PrecedenceTable
    {
        public enum Operator
        {
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

        public enum BindingPower
        {
            Higher,
            Lower,
            Ambiguous,
        }


        public static Operator? TryGetPrefixOperator(TokenKind kind)
            => kind switch
            {
                TokenKind.Minus => Operator.Negate,
                TokenKind.NotKw => Operator.LogicNot,
                _ => null
            };

        public static Operator? TryGetInfixOperator(TokenKind kind) => kind switch
        {
            TokenKind.Dot => Operator.Dot,
            TokenKind.OpenParen => Operator.ArgList,

            TokenKind.Star or TokenKind.Slash => Operator.Factor,
            TokenKind.Plus or TokenKind.Minus => Operator.Sum,

            TokenKind.LessThan or TokenKind.LessThanEqual or TokenKind.GreaterThan or TokenKind.GreaterThanEqual
                or TokenKind.DoubleEqual or TokenKind.BangEqual => Operator.Comparison,

            TokenKind.AndKw => Operator.LogicAnd,
            TokenKind.OrKw => Operator.LogicOr,

            _ => null
        };

        public static BindingPower RightBindingPower(Operator left, Operator right)
        {
            if (left == right && left is Operator.Comparison)
                return BindingPower.Ambiguous;

            if ((int)left < (int)right)
                return BindingPower.Higher;
            else
                return BindingPower.Lower;
        }
    }
}