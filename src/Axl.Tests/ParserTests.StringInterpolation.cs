using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests;

public partial class ParserTests
{
    public sealed class StringInterpolation
    {
        [Fact]
        public void SingleLine_1()
            => InlineSnapshot.Validate(Tree("""
                                            "Hello {1+2} World";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '2'
                · · · '}'
                · · StringText ' World'
                · · '"'
                · ';'
                """);
        [Fact]
        public void SingleLine_2()
            => InlineSnapshot.Validate(Tree("""
                                            "Hello {1+2}{a} World";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '2'
                · · · '}'
                · · StringInterpolation
                · · · '{'
                · · · Identifier 'a'
                · · · '}'
                · · StringText ' World'
                · · '"'
                · ';'
                """);
        [Fact]
        public void SingleLine_3()
            => InlineSnapshot.Validate(Tree("""
                                            "{1+2}{a}";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '2'
                · · · '}'
                · · StringInterpolation
                · · · '{'
                · · · Identifier 'a'
                · · · '}'
                · · '"'
                · ';'
                """);

        [Fact]
        public void Empty_SingleLine()
            => InlineSnapshot.Validate(Tree("""
                                            "{}";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringInterpolation '{' '}'
                · · '"'
                · ';'
                """);
        [Fact]
        public void Empty_MultiLine_1()
            => InlineSnapshot.Validate(Tree("""
                                            "{
                                            }";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringInterpolation '{' '}'
                · · '"'
                · ';'
                """);
        [Fact]
        public void Empty_MultiLine_2()
            => InlineSnapshot.Validate(Tree("""
                                            "Hello {
                                            }";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation '{' '}'
                · · '"'
                · ';'
                """);
        [Fact]
        public void Empty_MultiLine_3()
            => InlineSnapshot.Validate(Tree("""
                                            "Hello {
                                            } World";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation '{' '}'
                · · StringText ' World'
                · · '"'
                · ';'
                """);
        
        
        [Fact]
        public void MultiLine()
            => InlineSnapshot.Validate(Tree("""
                                            "Hello { 1 +
                                                2 }";
                                            """), """
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '2'
                · · · '}'
                · · '"'
                · ';'
                """);
    }
}