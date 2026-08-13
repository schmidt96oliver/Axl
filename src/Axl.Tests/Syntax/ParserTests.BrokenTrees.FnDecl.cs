using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class FnDecl
        {
            [Fact]
            public void UnclosedParamList_1()
                => InlineSnapshot.Validate(Tree("fn Foo( { }"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList '('
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_2()
                => InlineSnapshot.Validate(Tree("fn Foo( ;"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList '('
                    · ';'
                    """);
            [Fact]
            public void UnclosedParamList_3()
                => InlineSnapshot.Validate(Tree("fn Foo( { };"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList '('
                    · BlockExpr '{' '}'
                    · ';'
                    """);
            [Fact]
            public void UnclosedParamList_4()
                => InlineSnapshot.Validate(Tree("fn Foo( -> i32 { }"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList '('
                    · '->'
                    · NativeTypeName 'i32'
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_5()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32,  { }"), """
                    ERROR MissingToken@[14, 14): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ','
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_6()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32, -> i32 { }"), """
                    ERROR MissingToken@[14, 14): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ','
                    · '->'
                    · NativeTypeName 'i32'
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_7()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32, @@ { }"), """
                    ERROR UnknownCharacters@[15, 16): Unknown character '@'.
                    ERROR UnknownCharacters@[16, 17): Unknown character '@'.
                    ERROR UnexpectedToken@[15, 17): Expected a parameter, got an invalid token.
                    ERROR MissingToken@[17, 17): Expected ')'.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ','
                    · · Error '@@'
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_8()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32, @@ -> string { }"), """
                    ERROR UnknownCharacters@[15, 16): Unknown character '@'.
                    ERROR UnknownCharacters@[16, 17): Unknown character '@'.
                    ERROR UnexpectedToken@[15, 17): Expected a parameter, got an invalid token.
                    ERROR MissingToken@[17, 17): Expected ')'.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ','
                    · · Error '@@'
                    · '->'
                    · NativeTypeName 'string'
                    · BlockExpr '{' '}'
                    """);
        }
    }
}