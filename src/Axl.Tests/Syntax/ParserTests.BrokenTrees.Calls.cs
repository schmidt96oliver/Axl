using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class Calls
        {
            [Fact]
            public void ArgList_ForgottenComma_1()
                => InlineSnapshot.Validate(Tree("Foo(a b);"), """
                    ERROR MissingToken@[5, 5): Expected ','.


                    ExprStmt
                    · CallExpr
                    · · IdName 'Foo'
                    · · ArgList
                    · · · '('
                    · · · Arg
                    · · · · IdName 'a'
                    · · · missing ','
                    · · · Arg
                    · · · · IdName 'b'
                    · · · ')'
                    · ';'
                    """);
            
            [Fact]
            public void ArgList_ForgottenComma_2()
                => InlineSnapshot.Validate(Tree("Foo(a+b.c() 1);"), """
                    ERROR MissingToken@[11, 11): Expected ','.


                    ExprStmt
                    · CallExpr
                    · · IdName 'Foo'
                    · · ArgList
                    · · · '('
                    · · · Arg
                    · · · · BinaryExpr
                    · · · · · IdName 'a'
                    · · · · · '+'
                    · · · · · CallExpr
                    · · · · · · GetMemberExpr
                    · · · · · · · IdName 'b'
                    · · · · · · · '.'
                    · · · · · · · IdName 'c'
                    · · · · · · ArgList '(' ')'
                    · · · missing ','
                    · · · Arg
                    · · · · NumberLiteral '1'
                    · · · ')'
                    · ';'
                    """);
            
            [Fact]
            public void ArgList_ForgottenComma_3()
                => InlineSnapshot.Validate(Tree("Foo(a b, c d);"), """
                    ERROR MissingToken@[5, 5): Expected ','.
                    ERROR MissingToken@[10, 10): Expected ','.


                    ExprStmt
                    · CallExpr
                    · · IdName 'Foo'
                    · · ArgList
                    · · · '('
                    · · · Arg
                    · · · · IdName 'a'
                    · · · missing ','
                    · · · Arg
                    · · · · IdName 'b'
                    · · · ','
                    · · · Arg
                    · · · · IdName 'c'
                    · · · missing ','
                    · · · Arg
                    · · · · IdName 'd'
                    · · · ')'
                    · ';'
                    """);
        }
    }
}