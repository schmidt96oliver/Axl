using Axl.Compiler;
using Axl.Compiler.Syntax;
using Meziantou.Framework.InlineSnapshotTesting;
using Shouldly;

namespace Axl.Tests;

public partial class ParserTests
{
    private static string Tree(string text)
    {
        var source = SourceFileView.FromText(text);
        var tree = Parser.Parse(source);

        return new Dump(source)
            .Add(tree.Diagnostics)
            .AddChildren(tree.Root, filterTrivia: true, filterEof: true)
            .ToString();
    }

    private static string SExpr(string text)
    {
        var source = SourceFileView.FromText(text);
        var tree = Parser.Parse(source);

        var exprStmt = tree.Root.Children[..^1]
            .ShouldHaveSingleItem()
            .ShouldBeOfType<SyntaxNode>();
        exprStmt.Kind.ShouldBe(SyntaxKind.ExprStmt);
        exprStmt.Children.Length.ShouldBeGreaterThan(0);
        var inner = exprStmt.Children[0].ShouldBeOfType<SyntaxNode>();
        
        return new Dump(source)
            .Add(tree.Diagnostics)
            .AddSExpr(inner)
            .ToString();
    }

    [Fact]
    public void TestOneSimpleTree()
        => InlineSnapshot.Validate(Tree("1==2==3;"), """
            ERROR InvalidOperatorChaining@[1, 3), [4, 6): Cannot chain '==' and '=='.


            ExprStmt
            · Error
            · · NumberLiteral '1'
            · · '=='
            · · NumberLiteral '2'
            · · '=='
            · · NumberLiteral '3'
            · ';'
            """);

    [Fact]
    public void SExprTest()
        => InlineSnapshot.Validate(SExpr("1+(4*3) and 5;"), 
            "((1 + (4 * 3)) and 5)");
    [Fact]
    public void SExprTestTree()
        => InlineSnapshot.Validate(Tree("1+(4*3) and 5;"), 
            """
            ExprStmt
            · BinaryExpr
            · · BinaryExpr
            · · · NumberLiteral '1'
            · · · '+'
            · · · GroupExpr
            · · · · '('
            · · · · BinaryExpr
            · · · · · NumberLiteral '4'
            · · · · · '*'
            · · · · · NumberLiteral '3'
            · · · · ')'
            · · 'and'
            · · NumberLiteral '5'
            · ';'
            """);
    [Fact]
    public void SExprChaining()
        => InlineSnapshot.Validate(SExpr("1 and (2 or 3) and not 4 and not not 5;"), 
            "(((1 and (2 or 3)) and (not 4)) and (not (not 5)))");

    [Fact]
    public void OtherSExpr()
        => InlineSnapshot.Validate(SExpr("1+;"), """
            ERROR MissingToken@[2, 3): Expected an expression.

            (1 +)
            """);
    [Fact]
    public void StringsGalore()
        => InlineSnapshot.Validate(Tree("""
                                         "Hello { ab "bla bla\n bla bla\nblabla"
                                         }
                                         """), """
            ERROR MissingToken@[12, 13): Expected '}'.
            ERROR UnclosedString@[39, 39): String has not been closed.
            ERROR MissingToken@[39, 39): Expected ';'.
            ERROR UnexpectedToken@[41, 42): Expected a statement, got '}'.


            ExprStmt
            · StringExpr
            · · '"'
            · · StringText 'Hello '
            · · StringInterpolation
            · · · '{'
            · · · Identifier 'ab'
            · · · Error '"' 'bla bla\n bla bla\nblabla' '"'
            Error '}'
            """);
}