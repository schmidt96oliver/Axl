using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

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
                    · · NameRef 'Foo'
                    · · ArgList
                    · · · '('
                    · · · Arg
                    · · · · NameRef 'a'
                    · · · ??','
                    · · · Arg
                    · · · · NameRef 'b'
                    · · · ')'
                    · ';'
                    """);
            
            [Fact]
            public void ArgList_ForgottenComma_2()
                => InlineSnapshot.Validate(Tree("Foo(a+b.c() 1);"), """
                    ERROR MissingToken@[11, 11): Expected ','.


                    ExprStmt
                    · CallExpr
                    · · NameRef 'Foo'
                    · · ArgList
                    · · · '('
                    · · · Arg
                    · · · · BinaryExpr
                    · · · · · NameRef 'a'
                    · · · · · '+'
                    · · · · · CallExpr
                    · · · · · · MemberAccessExpr
                    · · · · · · · NameRef 'b'
                    · · · · · · · '.'
                    · · · · · · · NameRef 'c'
                    · · · · · · ArgList '(' ')'
                    · · · ??','
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
                    · · NameRef 'Foo'
                    · · ArgList
                    · · · '('
                    · · · Arg
                    · · · · NameRef 'a'
                    · · · ??','
                    · · · Arg
                    · · · · NameRef 'b'
                    · · · ','
                    · · · Arg
                    · · · · NameRef 'c'
                    · · · ??','
                    · · · Arg
                    · · · · NameRef 'd'
                    · · · ')'
                    · ';'
                    """);
        }
    }
}