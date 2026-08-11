using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public sealed class Precedence
    {
        [Fact]
        public void Algebraic_1()
            => InlineSnapshot.Validate(SExpr("1 + 2*3 - 4/5;"), 
                "((1 + (2 * 3)) - (4 / 5))");
        [Fact]
        public void Algebraic_2()
            => InlineSnapshot.Validate(SExpr("-1 + -3 * -4 / -2 ;"), 
                "((- 1) + (((- 3) * (- 4)) / (- 2)))");

        [Fact]
        public void Group_1()
            => InlineSnapshot.Validate(SExpr("(1+2)*3;"), "((1 + 2) * 3)");
        [Fact]
        public void Group_2()
            => InlineSnapshot.Validate(SExpr("not (1+2).a*3 == -1;"), "(not ((((1 + 2) . a) * 3) == (- 1)))");
        [Fact]
        public void Group_3()
            => InlineSnapshot.Validate(SExpr("(1 == 2) != 3 and (4 or 5);"), "(((1 == 2) != 3) and (4 or 5))");
        
        [Fact]
        public void Comparison_1()
            => InlineSnapshot.Validate(SExpr("1 + 2*3 == -2.1;"), "((1 + (2 * 3)) == (- 2.1))");
        
        [Fact]
        public void Boolean_1()
            => InlineSnapshot.Validate(SExpr("not 1 < 2 and not 3 + 4 >= 5;"), "((not (1 < 2)) and (not ((3 + 4) >= 5)))");
        [Fact]
        public void Boolean_2()
            => InlineSnapshot.Validate(SExpr("not 1 == 2 or not 3 + 4 != 5;"), "((not (1 == 2)) or (not ((3 + 4) != 5)))");

        [Fact]
        public void GetMember_1()
            => InlineSnapshot.Validate(SExpr("a.b.c;"), "((a . b) . c)");
        [Fact]
        public void GetMember_2()
            => InlineSnapshot.Validate(SExpr("1+a.b;"), "(1 + (a . b))");
        [Fact]
        public void GetMember_3()
            => InlineSnapshot.Validate(SExpr("1+4*a.b;"), "(1 + (4 * (a . b)))");
        [Fact]
        public void GetMember_4()
            => InlineSnapshot.Validate(SExpr("-a.b;"), "(- (a . b))");
        
        [Fact]
        public void Comparison_DoesNotChain_1()
            => InlineSnapshot.Validate(SExpr("1 == 2 == 3;"), """
                ERROR InvalidOperatorChaining@[2, 4), [7, 9): Cannot chain '==' and '=='.

                (1 == 2 == 3)
                """);
        [Fact]
        public void Comparison_DoesNotChain_2()
            => InlineSnapshot.Validate(SExpr("1 == 2 < 3;"), """
                ERROR InvalidOperatorChaining@[2, 4), [7, 8): Cannot chain '==' and '<'.

                (1 == 2 < 3)
                """);
        [Fact]
        public void Comparison_DoesNotChain_3()
            => InlineSnapshot.Validate(SExpr("1 == 2 < 3 <= 4 != 5;"), """
                ERROR InvalidOperatorChaining@[2, 4), [7, 8), [11, 13), [16, 18): Cannot chain '==', '<', '<=' and '!='.

                (1 == 2 < 3 <= 4 != 5)
                """);

        [Fact]
        public void Comparison_DoesNotChain_KeepsInnerExpr()
            => InlineSnapshot.Validate(Tree("1 == 1+2*3 != a.b < -4"), """
                ERROR InvalidOperatorChaining@[2, 4), [11, 13), [18, 19): Cannot chain '==', '!=' and '<'.
                ERROR MissingToken@[22, 22): Expected ';'.


                ExprStmt
                · Error
                · · NumberLiteral '1'
                · · '=='
                · · BinaryExpr
                · · · NumberLiteral '1'
                · · · '+'
                · · · BinaryExpr
                · · · · NumberLiteral '2'
                · · · · '*'
                · · · · NumberLiteral '3'
                · · '!='
                · · GetMemberExpr
                · · · IdName 'a'
                · · · '.'
                · · · IdName 'b'
                · · '<'
                · · UnaryExpr
                · · · '-'
                · · · NumberLiteral '4'
                """);

        [Fact]
        public void AndOr_DoNotChain_1()
            => InlineSnapshot.Validate(SExpr("1 and 2 or 3;"), """
                ERROR InvalidOperatorChaining@[2, 5), [8, 10): Cannot chain 'and' and 'or'.

                (1 and 2 or 3)
                """);
        [Fact]
        public void AndOr_DoNotChain_2()
            => InlineSnapshot.Validate(SExpr("1 and 2 and 3 or 3;"), """
                ERROR InvalidOperatorChaining@[8, 11), [14, 16): Cannot chain 'and' and 'or'.

                ((1 and 2) and 3 or 3)
                """);
        [Fact]
        public void AndOr_DoNotChain_3()
            => InlineSnapshot.Validate(SExpr("1 or 2 and 3 or 3;"), """
                ERROR InvalidOperatorChaining@[2, 4), [7, 10), [13, 15): Cannot chain 'or', 'and' and 'or'.

                (1 or 2 and 3 or 3)
                """);
    }
}