using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class FunDecl
        {
            [Fact]
            public void UnclosedParamList_1()
                => InlineSnapshot.Validate(Tree("fun Foo( { }"), """
                    ERROR MissingToken@[8, 8): Expected ')'.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_2()
                => InlineSnapshot.Validate(Tree("fun Foo( ;"), """
                    ERROR MissingToken@[8, 8): Expected ')'.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · ??')'
                    · FunBody
                    · · BlockExpr
                    · · · ??'{'
                    · · · ??'}'
                    · · ';'
                    """);
            [Fact]
            public void UnclosedParamList_3()
                => InlineSnapshot.Validate(Tree("fun Foo( { };"), """
                    ERROR MissingToken@[8, 8): Expected ')'.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    · · ';'
                    """);
            [Fact]
            public void UnclosedParamList_4()
                => InlineSnapshot.Validate(Tree("fun Foo(: i32 { }"), """
                    ERROR MissingToken@[8, 8): Expected an identifier.
                    ERROR MissingToken@[13, 13): Expected ')'.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_5()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32,  { }"), """
                    ERROR MissingToken@[15, 15): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_6()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32,: i32 { }"), """
                    ERROR MissingToken@[15, 15): Expected an identifier.
                    ERROR MissingToken@[20, 20): Expected ')'.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_7()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32, @@ { }"), """
                    ERROR UnexpectedToken@[16, 18): Expected a parameter, got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_8()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32, @@: string { }"), """
                    ERROR UnexpectedToken@[16, 18): Expected a parameter, got unknown characters.
                    ERROR MissingToken@[26, 26): Expected ')'.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'string'
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void UnclosedParamList_9()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32,  = 1;"), """
                    ERROR MissingToken@[15, 15): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · '='
                    · · NumberLiteral '1'
                    · · ';'
                    """);
            
            [Fact]
            public void UnclosedParamList_10()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32,  = 1;"), """
                    ERROR MissingToken@[15, 15): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · '='
                    · · NumberLiteral '1'
                    · · ';'
                    """);
            
            
            [Fact]
            public void ParamList_ForgottenComma_1()
                => InlineSnapshot.Validate(Tree("fun Foo(a b) { }"), """
                    ERROR MissingToken@[9, 9): Expected ','.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_ForgottenComma_2()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32 b: string) { }"), """
                    ERROR MissingToken@[14, 14): Expected ','.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'string'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_ForgottenComma_3()
                => InlineSnapshot.Validate(Tree("fun Foo(a: i32 b c: bool, d) { }"), """
                    ERROR MissingToken@[14, 14): Expected ','.
                    ERROR MissingToken@[16, 16): Expected ','.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · ??','
                    · · Param
                    · · · IdName 'c'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'bool'
                    · · ','
                    · · Param
                    · · · IdName 'd'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            
            [Fact]
            public void Param_ForgottenId()
                => InlineSnapshot.Validate(Tree("fun A( : i32) { } "), """
                    ERROR MissingToken@[6, 6): Expected an identifier.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenTypeName()
                => InlineSnapshot.Validate(Tree("fun A(a : ) { } "), """
                    ERROR MissingToken@[9, 9): Expected a type name.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · Path
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenColon_FollowedByNativeTypeName()
                => InlineSnapshot.Validate(Tree("fun A(a i32) { } "), """
                    ERROR MissingToken@[7, 7): Expected ':'.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · NativeTypeName 'i32'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenColon_FollowedByQualifiedName_1()
                => InlineSnapshot.Validate(Tree("fun A(a a.b) { } "), """
                    ERROR MissingToken@[7, 7): Expected ':'.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · Path
                    · · · · · IdName 'a'
                    · · · · · '.'
                    · · · · · IdName 'b'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenColon_FollowedByQualifiedName_2()
                => InlineSnapshot.Validate(Tree("fun A(a a.) { } "), """
                    ERROR MissingToken@[7, 7): Expected ':'.
                    ERROR MissingToken@[10, 10): Expected an identifier.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · Path
                    · · · · · IdName 'a'
                    · · · · · '.'
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_OnlyNativeTypeName()
                => InlineSnapshot.Validate(Tree("fun A(f32) { } "), """
                    ERROR MissingToken@[6, 6): Expected an identifier.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · NativeTypeName 'f32'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            
            [Fact]
            public void ParamList_ForgottenItem_1()
                => InlineSnapshot.Validate(Tree("fun Foo( , ) { }"), """
                    ERROR MissingToken@[8, 8): Expected a parameter.
                    ERROR MissingToken@[10, 10): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            [Fact]
            public void ParamList_ForgottenItem_2()
                => InlineSnapshot.Validate(Tree("fun Foo( , , ) { }"), """
                    ERROR MissingToken@[8, 8): Expected a parameter.
                    ERROR MissingToken@[10, 10): Expected a parameter.
                    ERROR MissingToken@[12, 12): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            [Fact]
            public void ParamList_ForgottenItem_3()
                => InlineSnapshot.Validate(Tree("fun Foo(a,) { }"), """
                    ERROR MissingToken@[10, 10): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            [Fact]
            public void NoParamList_1()
                => InlineSnapshot.Validate(Tree("fun A { }"), """
                    ERROR MissingToken@[5, 5): Expected parameters ('(').


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void NoParamList_2()
                => InlineSnapshot.Validate(Tree("fun A = 1;"), """
                    ERROR MissingToken@[5, 5): Expected parameters ('(').


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · FunBody
                    · · '='
                    · · NumberLiteral '1'
                    · · ';'
                    """);
            
            [Fact]
            public void NoParamList_3()
                => InlineSnapshot.Validate(Tree("fun A: i32 { }"), """
                    ERROR MissingToken@[5, 5): Expected parameters ('(').


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · TypeAnnotationClause
                    · · ':'
                    · · NativeTypeName 'i32'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            

            [Fact]
            public void ParamList_Garbage_1()
                => InlineSnapshot.Validate(Tree("fun Foo(a @@ b) { }"), """
                    ERROR UnexpectedToken@[10, 12): Expected ',', got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · Garbage '@@'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_2()
                => InlineSnapshot.Validate(Tree("fun Foo(@@ a, b) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName 'a'
                    · · ','
                    · · Param
                    · · · IdName 'b'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_3()
                => InlineSnapshot.Validate(Tree("fun Foo(@@, a, b) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'a'
                    · · ','
                    · · Param
                    · · · IdName 'b'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_4()
                => InlineSnapshot.Validate(Tree("fun Foo(@@) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_5()
                => InlineSnapshot.Validate(Tree("fun Foo(@@, ) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.
                    ERROR MissingToken@[11, 11): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_6()
                => InlineSnapshot.Validate(Tree("fun Foo(a @@, @@) { }"), """
                    ERROR UnexpectedToken@[10, 12): Expected ',', got unknown characters.
                    ERROR UnexpectedToken@[14, 16): Expected a parameter, got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · Garbage '@@'
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_7()
                => InlineSnapshot.Validate(Tree("fun Foo(@@, a, b @@) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.
                    ERROR UnexpectedToken@[17, 19): Expected ')', got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'a'
                    · · ','
                    · · Param
                    · · · IdName 'b'
                    · · Garbage '@@'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_8()
                => InlineSnapshot.Validate(Tree("fun Foo(@@,@@) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.
                    ERROR UnexpectedToken@[11, 13): Expected a parameter, got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            
        }
    }
}