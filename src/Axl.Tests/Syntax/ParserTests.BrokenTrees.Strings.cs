using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class Strings
        {
            [Fact]
            public void Typing_ClosingBraceOnNextLine_1()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello
                                                }
                                                """), """
                    ERROR MissingToken@[22, 22): Expected '"'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ClosingBraceOnNextLine_2()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello {
                                                }
                                                """), """
                    ERROR MissingToken@[24, 24): Expected '}'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello '
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · ??'}'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ClosingBraceOnNextLine_3()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello { 1
                                                }
                                                """), """
                    ERROR MissingToken@[26, 26): Expected '}'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello '
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · NumberLiteral '1'
                    · · · · · · ??'}'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ClosingBraceOnNextLine_4()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello { 1 +
                                                }
                                                """), """
                    ERROR MissingToken@[28, 28): Expected an expression.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello '
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · BinaryExpr
                    · · · · · · · NumberLiteral '1'
                    · · · · · · · '+'
                    · · · · · · · IdName
                    · · · · · · · · ??ID
                    · · · · · · ??'}'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ClosingBraceOnNextLine_5()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello { 1 + 6
                                                }
                                                """), """
                    ERROR MissingToken@[30, 30): Expected '}'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello '
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · BinaryExpr
                    · · · · · · · NumberLiteral '1'
                    · · · · · · · '+'
                    · · · · · · · NumberLiteral '6'
                    · · · · · · ??'}'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ClosingBraceOnNextLine_6()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello { 1 + 6 }
                                                }
                                                """), """
                    ERROR MissingToken@[32, 32): Expected '"'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello '
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · BinaryExpr
                    · · · · · · · NumberLiteral '1'
                    · · · · · · · '+'
                    · · · · · · · NumberLiteral '6'
                    · · · · · · '}'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ExprOnNextLine_1()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello
                                                    1+2;
                                                }
                                                """), """
                    ERROR MissingToken@[22, 22): Expected '"'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · ExprStmt
                    · · · · BinaryExpr
                    · · · · · NumberLiteral '1'
                    · · · · · '+'
                    · · · · · NumberLiteral '2'
                    · · · · ';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ExprOnNextLine_2()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello { 1
                                                    1+2;
                                                }
                                                """), """
                    ERROR MissingToken@[26, 26): Expected '}'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello '
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · NumberLiteral '1'
                    · · · · · · ??'}'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · ExprStmt
                    · · · · BinaryExpr
                    · · · · · NumberLiteral '1'
                    · · · · · '+'
                    · · · · · NumberLiteral '2'
                    · · · · ';'
                    · · · '}'
                    """);

            [Fact]
            public void Typing_ExprOnNextLine_3()
                => InlineSnapshot.Validate(Tree("""
                                                fun a()
                                                {
                                                    "Hello { 1 + 6 } World
                                                    1+2;
                                                }
                                                """), """
                    ERROR MissingToken@[38, 38): Expected '"'.


                    FunDecl
                    · 'fun'
                    · IdName 'a'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringText 'Hello '
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · BinaryExpr
                    · · · · · · · NumberLiteral '1'
                    · · · · · · · '+'
                    · · · · · · · NumberLiteral '6'
                    · · · · · · '}'
                    · · · · · StringText ' World'
                    · · · · · ??'"'
                    · · · · ??';'
                    · · · ExprStmt
                    · · · · BinaryExpr
                    · · · · · NumberLiteral '1'
                    · · · · · '+'
                    · · · · · NumberLiteral '2'
                    · · · · ';'
                    · · · '}'
                    """);


            [Fact]
            public void Typing_OpeningBraceOnNextLine_1()
                => InlineSnapshot.Validate(Tree("""
                                                "
                                                { }
                                                """), """
                    ERROR MissingToken@[1, 1): Expected '"'.


                    ExprStmt
                    · StringExpr
                    · · '"'
                    · · ??'"'
                    · ??';'
                    ExprStmt
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void Typing_OpeningBraceOnNextLine_2()
                => InlineSnapshot.Validate(Tree("""
                                                "Text
                                                { }
                                                """), """
                    ERROR MissingToken@[5, 5): Expected '"'.


                    ExprStmt
                    · StringExpr
                    · · '"'
                    · · StringText 'Text'
                    · · ??'"'
                    · ??';'
                    ExprStmt
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void Typing_OpeningBraceOnNextLine_3()
                => InlineSnapshot.Validate(Tree("""
                                                "Text {}
                                                { }
                                                """), """
                    ERROR MissingToken@[8, 8): Expected '"'.


                    ExprStmt
                    · StringExpr
                    · · '"'
                    · · StringText 'Text '
                    · · StringInterpolation '{' '}'
                    · · ??'"'
                    · ??';'
                    ExprStmt
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void OpenInterpolationInCall_FollowedByVar()
                // EXPECT: One error; VarDecl is global
                => InlineSnapshot.Validate(Tree("""
                                                Standard.PrintLine("{ 
                                                var a = 2;
                                                """), """
                    ERROR MissingToken@[21, 21): Expected '}'.


                    ExprStmt
                    · CallExpr
                    · · GetMemberExpr
                    · · · IdName 'Standard'
                    · · · '.'
                    · · · IdName 'PrintLine'
                    · · ArgList
                    · · · '('
                    · · · Arg
                    · · · · StringExpr
                    · · · · · '"'
                    · · · · · StringInterpolation
                    · · · · · · '{'
                    · · · · · · ??'}'
                    · · · · · ??'"'
                    · · · ??')'
                    · ??';'
                    VarDecl
                    · 'var'
                    · IdName 'a'
                    · InitializerClause
                    · · '='
                    · · NumberLiteral '2'
                    · ';'
                    """);
        }
    }
}