using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    public sealed class Precedence
    {
        [Fact]
        public void Range_1()
            => InlineSnapshot.Validate(SExpr("1..2;"), """
                (1 .. 2)
                """);
        
        [Fact]
        public void Range_2()
            => InlineSnapshot.Validate(SExpr("1+2..-2-4*6;"), """
                ((1 + (2 .. (- 2))) - (4 * 6))
                """);
        [Fact]
        public void Range_3()
            => InlineSnapshot.Validate(SExpr("a() .. b().c;"), """
                ((a (( ))) .. ((b (( ))) . c))
                """);
        
        [Fact]
        public void Algebraic_1()
            => InlineSnapshot.Validate(SExpr("1 + 2*3 - 4/5;"), 
                "((1 + (2 * 3)) - (4 / 5))");
        [Fact]
        public void Algebraic_2()
            => InlineSnapshot.Validate(SExpr("-1 + -3 * -4 / -2 ;"), 
                "((- 1) + (((- 3) * (- 4)) / (- 2)))");

        [Fact]
        public void Assign_Lowest_1()
            => InlineSnapshot.Validate(SExpr("a.b() = !c && d;"), "(((a . b) (( ))) = ((! c) && d))");

        [Fact]
        public void Assign_Lowest_2()
            => InlineSnapshot.Validate(SExpr("a.b() += !c && d;"), "(((a . b) (( ))) += ((! c) && d))");
        
        [Fact]
        public void Assign_Lowest_3()
            => InlineSnapshot.Validate(SExpr("a.b() += -c;"), "(((a . b) (( ))) += (- c))");

        [Fact]
        public void Assign_RightAssociative_1()
            => InlineSnapshot.Validate(SExpr("a = b = c = d;"), "(a = (b = (c = d)))");

        [Fact]
        public void Assign_RightAssociative_2()
            => InlineSnapshot.Validate(SExpr("a += b *= c /= d -= e = f;"), "(a += (b *= (c /= (d -= (e = f)))))");
        
        [Fact]
        public void Group_1()
            => InlineSnapshot.Validate(SExpr("(1+2)*3;"), "((1 + 2) * 3)");
        [Fact]
        public void Group_2()
            => InlineSnapshot.Validate(SExpr("! (1+2).a*3 == -1;"), "(((! ((1 + 2) . a)) * 3) == (- 1))");
        [Fact]
        public void Group_3()
            => InlineSnapshot.Validate(SExpr("(1 == 2) != 3 && (4 || 5);"), "(((1 == 2) != 3) && (4 || 5))");
        
        [Fact]
        public void Comparison_1()
            => InlineSnapshot.Validate(SExpr("1 + 2*3 == -2.1;"), "((1 + (2 * 3)) == (- 2.1))");
        
        [Fact]
        public void Boolean_1()
            => InlineSnapshot.Validate(SExpr("!1 < 2 && !3 + 4 >= 5;"), "(((! 1) < 2) && (((! 3) + 4) >= 5))");
        [Fact]
        public void Boolean_2()
            => InlineSnapshot.Validate(SExpr("!1 == 2 || !3 + 4 != 5;"), "(((! 1) == 2) || (((! 3) + 4) != 5))");

        [Fact]
        public void MemberAccess_1()
            => InlineSnapshot.Validate(SExpr("a.b.c;"), "((a . b) . c)");
        [Fact]
        public void MemberAccess_2()
            => InlineSnapshot.Validate(SExpr("1+a.b;"), "(1 + (a . b))");
        [Fact]
        public void MemberAccess_3()
            => InlineSnapshot.Validate(SExpr("1+4*a.b;"), "(1 + (4 * (a . b)))");
        [Fact]
        public void MemberAccess_4()
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
            => InlineSnapshot.Validate(Tree("1 == 1+2*3 != a.b < -4;"), """
                ERROR InvalidOperatorChaining@[2, 4), [11, 13), [18, 19): Cannot chain '==', '!=' and '<'.


                ExprStmt
                · ErrorExpr
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
                · · MemberAccessExpr
                · · · NameRef 'a'
                · · · '.'
                · · · NameRef 'b'
                · · '<'
                · · UnaryExpr
                · · · '-'
                · · · NumberLiteral '4'
                · ';'
                """);

        [Fact]
        public void AndOr_1()
            => InlineSnapshot.Validate(SExpr("1 && 2 || 3;"), "((1 && 2) || 3)");
        [Fact]
        public void AndOr_2()
            => InlineSnapshot.Validate(SExpr("1 && 2 && 3 || 3;"), "(((1 && 2) && 3) || 3)");
        [Fact]
        public void AndOr_3()
            => InlineSnapshot.Validate(SExpr("1 || 2 && 3 || 3;"), "((1 || (2 && 3)) || 3)");
    }
}