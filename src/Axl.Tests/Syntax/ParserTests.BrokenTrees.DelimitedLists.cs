using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class DelimitedLists
        {
            [Fact]
            public void ForgottenComma_ParamList_NoTypeAnnotation()
                => InlineSnapshot.Validate(Tree("fn Foo(a b) { }"), """
                    ERROR MissingToken@[8, 8): Expected ','.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · Param
                    · · · IdName 'b'
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ForgottenComma_ParamList_WithTypeAnnotation()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32 b: string) { }"), """
                    ERROR MissingToken@[13, 13): Expected ','.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · Param
                    · · · IdName 'b'
                    · · · ':'
                    · · · NativeTypeName 'string'
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ForgottenComma_ParamList_Mixed()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32 b c: bool, d) { }"), """
                    ERROR MissingToken@[13, 13): Expected ','.
                    ERROR MissingToken@[15, 15): Expected ','.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · Param
                    · · · IdName 'b'
                    · · Param
                    · · · IdName 'c'
                    · · · ':'
                    · · · NativeTypeName 'bool'
                    · · ','
                    · · Param
                    · · · IdName 'd'
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
            
            
            [Fact]
            public void ForgottenComma_ArgList_1()
                => InlineSnapshot.Validate(Tree("Foo(a b);"), """
                    ERROR MissingToken@[5, 5): Expected ','.


                    ExprStmt
                    · CallExpr
                    · · IdName 'Foo'
                    · · ArgList
                    · · · '('
                    · · · IdName 'a'
                    · · · IdName 'b'
                    · · · ')'
                    · ';'
                    """);
            
            [Fact]
            public void ForgottenComma_ArgList_2()
                => InlineSnapshot.Validate(Tree("Foo(a+b.c() 1);"), """
                    ERROR MissingToken@[11, 11): Expected ','.


                    ExprStmt
                    · CallExpr
                    · · IdName 'Foo'
                    · · ArgList
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · '+'
                    · · · · CallExpr
                    · · · · · GetMemberExpr
                    · · · · · · IdName 'b'
                    · · · · · · '.'
                    · · · · · · IdName 'c'
                    · · · · · ArgList '(' ')'
                    · · · NumberLiteral '1'
                    · · · ')'
                    · ';'
                    """);
            
            [Fact]
            public void ForgottenComma_ArgList_3()
                => InlineSnapshot.Validate(Tree("Foo(a b, c d);"), """
                    ERROR MissingToken@[5, 5): Expected ','.
                    ERROR MissingToken@[10, 10): Expected ','.


                    ExprStmt
                    · CallExpr
                    · · IdName 'Foo'
                    · · ArgList
                    · · · '('
                    · · · IdName 'a'
                    · · · IdName 'b'
                    · · · ','
                    · · · IdName 'c'
                    · · · IdName 'd'
                    · · · ')'
                    · ';'
                    """);


            [Fact]
            public void Garbage_ParamList_1()
                => InlineSnapshot.Validate(Tree("fn Foo(a @@ b) { }"), """
                    ERROR UnknownCharacters@[9, 10): Unknown character '@'.
                    ERROR UnknownCharacters@[10, 11): Unknown character '@'.
                    ERROR UnexpectedToken@[9, 11): Expected ',', got an invalid token.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · Error '@@'
                    · · Param
                    · · · IdName 'b'
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Garbage_ParamList_2()
                => InlineSnapshot.Validate(Tree("fn Foo(@@ a, b) { }"));
            
            [Fact]
            public void Garbage_ParamList_3()
                => InlineSnapshot.Validate(Tree("fn Foo(@@, a, b) { }"));
            
            [Fact]
            public void Garbage_ParamList_4()
                => InlineSnapshot.Validate(Tree("fn Foo(@@, a, b @@) { }"));
            
            [Fact]
            public void Garbage_ParamList_5()
                => InlineSnapshot.Validate(Tree("fn Foo(@@, a, b, @@) { }"));


            [Fact]
            public void ForgottenItem_ParamList_1()
                => InlineSnapshot.Validate(Tree("fn Foo( , ) { }"), """
                    ERROR MissingToken@[7, 7): Expected an identifier.
                    ERROR MissingToken@[9, 9): Expected an identifier.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList '(' ',' ')'
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void ForgottenItem_ParamList_2()
                => InlineSnapshot.Validate(Tree("fn Foo( , , ) { }"), """
                    ERROR MissingToken@[7, 7): Expected an identifier.
                    ERROR MissingToken@[9, 9): Expected an identifier.
                    ERROR MissingToken@[11, 11): Expected an identifier.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList '(' ',' ',' ')'
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void ForgottenItem_ParamList_3()
                => InlineSnapshot.Validate(Tree("fn Foo(a,) { }"), """
                    ERROR MissingToken@[9, 9): Expected an identifier.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · ','
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
        }
    }
}