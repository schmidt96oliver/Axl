using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests;

public partial class ParserTests
{
    public sealed class StringInterpolationResilience
    {
        [Fact]
        public void Typing_ClosingBraceOnNextLine_1()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[21, 21): String has not been closed.
                ERROR MissingToken@[21, 21): Expected ';'.
                ERROR UnexpectedToken@[23, 24): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello'
                Error '}'
                """);
        [Fact]
        public void Typing_ClosingBraceOnNextLine_2()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello {
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[23, 23): String has not been closed.
                ERROR MissingToken@[23, 23): Expected ';'.
                ERROR UnexpectedToken@[25, 26): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation '{'
                Error '}'
                """);
        [Fact]
        public void Typing_ClosingBraceOnNextLine_3()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[25, 25): String has not been closed.
                ERROR MissingToken@[25, 25): Expected ';'.
                ERROR UnexpectedToken@[27, 28): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · NumberLiteral '1'
                Error '}'
                """);
        [Fact]
        public void Typing_ClosingBraceOnNextLine_4()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 +
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR MissingToken@[27, 27): Expected an expression.
                ERROR UnclosedString@[27, 27): String has not been closed.
                ERROR MissingToken@[27, 27): Expected ';'.
                ERROR UnexpectedToken@[29, 30): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                Error '}'
                """);
        [Fact]
        public void Typing_ClosingBraceOnNextLine_5()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 + 6
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[29, 29): String has not been closed.
                ERROR MissingToken@[29, 29): Expected ';'.
                ERROR UnexpectedToken@[31, 32): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '6'
                Error '}'
                """);
        [Fact]
        public void Typing_ClosingBraceOnNextLine_6()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 + 6 }
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[31, 31): String has not been closed.
                ERROR MissingToken@[31, 31): Expected ';'.
                ERROR UnexpectedToken@[33, 34): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '6'
                · · · '}'
                Error '}'
                """);
        [Fact]
        public void Typing_ClosingBraceOnNextLine_7()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 + 6 }";
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnexpectedToken@[35, 36): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '6'
                · · · '}'
                · · '"'
                · ';'
                Error '}'
                """);
        
        [Fact]
        public void Typing_ExprOnNextLine_1()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[21, 21): String has not been closed.
                ERROR MissingToken@[21, 21): Expected ';'.
                ERROR UnexpectedToken@[33, 34): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello'
                ExprStmt
                · BinaryExpr
                · · NumberLiteral '1'
                · · '+'
                · · NumberLiteral '2'
                · ';'
                Error '}'
                """);
        [Fact]
        public void Typing_ExprOnNextLine_2()
            // Here, it is expected, that the next expression is eaten.
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello {
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR MissingToken@[32, 33): Expected '}'.
                ERROR UnclosedString@[33, 33): String has not been closed.
                ERROR MissingToken@[33, 33): Expected ';'.
                ERROR UnexpectedToken@[35, 36): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
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
                · · · Error ';'
                Error '}'
                """);
        [Fact]
        public void Typing_ExprOnNextLine_3()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR MissingToken@[25, 25): Expected '}'.
                ERROR UnclosedString@[25, 25): String has not been closed.
                ERROR MissingToken@[25, 25): Expected ';'.
                ERROR UnexpectedToken@[37, 38): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · NumberLiteral '1'
                ExprStmt
                · BinaryExpr
                · · NumberLiteral '1'
                · · '+'
                · · NumberLiteral '2'
                · ';'
                Error '}'
                """);
        [Fact]
        public void Typing_ExprOnNextLine_4()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 +
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR MissingToken@[36, 37): Expected '}'.
                ERROR UnclosedString@[37, 37): String has not been closed.
                ERROR MissingToken@[37, 37): Expected ';'.
                ERROR UnexpectedToken@[39, 40): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · BinaryExpr
                · · · · · NumberLiteral '1'
                · · · · · '+'
                · · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '2'
                · · · Error ';'
                Error '}'
                """);
        [Fact]
        public void Typing_ExprOnNextLine_5()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 + 6
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR MissingToken@[29, 29): Expected '}'.
                ERROR UnclosedString@[29, 29): String has not been closed.
                ERROR MissingToken@[29, 29): Expected ';'.
                ERROR UnexpectedToken@[41, 42): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '6'
                ExprStmt
                · BinaryExpr
                · · NumberLiteral '1'
                · · '+'
                · · NumberLiteral '2'
                · ';'
                Error '}'
                """);
        [Fact]
        public void Typing_ExprOnNextLine_6()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 + 6 }
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[31, 31): String has not been closed.
                ERROR MissingToken@[31, 31): Expected ';'.
                ERROR UnexpectedToken@[43, 44): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '6'
                · · · '}'
                ExprStmt
                · BinaryExpr
                · · NumberLiteral '1'
                · · '+'
                · · NumberLiteral '2'
                · ';'
                Error '}'
                """);
        [Fact]
        public void Typing_ExprOnNextLine_7()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 + 6 } World
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnclosedString@[37, 37): String has not been closed.
                ERROR MissingToken@[37, 37): Expected ';'.
                ERROR UnexpectedToken@[49, 50): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '6'
                · · · '}'
                · · StringText ' World'
                ExprStmt
                · BinaryExpr
                · · NumberLiteral '1'
                · · '+'
                · · NumberLiteral '2'
                · ';'
                Error '}'
                """);
        [Fact]
        public void Typing_ExprOnNextLine_8()
            => InlineSnapshot.Validate(Tree("""
                                            fn a()
                                            {
                                                "Hello { 1 + 6 } World";
                                                1+2;
                                            }
                                            """), """
                ERROR UnexpectedToken@[0, 2): Expected a statement, got 'fn'.
                ERROR MissingToken@[5, 6): Expected an expression.
                ERROR MissingToken@[5, 6): Expected ';'.
                ERROR UnexpectedToken@[5, 6): Expected a statement, got ')'.
                ERROR UnexpectedToken@[8, 9): Expected a statement, got '{'.
                ERROR UnexpectedToken@[51, 52): Expected a statement, got '}'.


                Error 'fn'
                ExprStmt
                · BinaryExpr
                · · Identifier 'a'
                · · '('
                Error ')'
                Error '{'
                ExprStmt
                · StringExpr
                · · '"'
                · · StringText 'Hello '
                · · StringInterpolation
                · · · '{'
                · · · BinaryExpr
                · · · · NumberLiteral '1'
                · · · · '+'
                · · · · NumberLiteral '6'
                · · · '}'
                · · StringText ' World'
                · · '"'
                · ';'
                ExprStmt
                · BinaryExpr
                · · NumberLiteral '1'
                · · '+'
                · · NumberLiteral '2'
                · ';'
                Error '}'
                """);
    }
}