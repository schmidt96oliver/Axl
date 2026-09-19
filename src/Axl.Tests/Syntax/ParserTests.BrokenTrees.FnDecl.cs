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
                => InlineSnapshot.Validate(Tree("fun Foo(: I32 { }"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_5()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32,  { }"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_6()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32,: I32 { }"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_7()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32, @@ { }"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_8()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32, @@: String { }"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · TypeName
                    · · · · · IdName 'String'
                    · · ??')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void UnclosedParamList_9()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32,  = 1;"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · '='
                    · · NumberLiteral '1'
                    · · ';'
                    """);
            
            [Fact]
            public void UnclosedParamList_10()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32,  = 1;"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ??')'
                    · FunBody
                    · · '='
                    · · NumberLiteral '1'
                    · · ';'
                    """);
            
            
            [Fact]
            public void ParamList_ForgottenComma_1()
                => InlineSnapshot.Validate(Tree("fun Foo(a b) { }"), """
                    ERROR MissingToken@[9, 9): Expected a type annotation.
                    ERROR MissingToken@[11, 11): Expected a type annotation.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_ForgottenComma_2()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32 b: String) { }"), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · TypeName
                    · · · · · IdName 'String'
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_ForgottenComma_3()
                => InlineSnapshot.Validate(Tree("fun Foo(a: I32 b c: Bool, d) { }"), """
                    ERROR MissingToken@[14, 14): Expected ','.
                    ERROR MissingToken@[16, 16): Expected a type annotation.
                    ERROR MissingToken@[27, 27): Expected a type annotation.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · TypeName
                    · · · · · IdName 'I32'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ??','
                    · · Param
                    · · · IdName 'c'
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · TypeName
                    · · · · · IdName 'Bool'
                    · · ','
                    · · Param
                    · · · IdName 'd'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            
            [Fact]
            public void Param_ForgottenId()
                => InlineSnapshot.Validate(Tree("fun A( : I32) { } "), """
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
                    · · · · TypeName
                    · · · · · IdName 'I32'
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
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
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
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
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
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            [Fact]
            public void ParamList_ForgottenItem_3()
                => InlineSnapshot.Validate(Tree("fun Foo(a,) { }"), """
                    ERROR MissingToken@[9, 9): Expected a type annotation.
                    ERROR MissingToken@[10, 10): Expected a parameter.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
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
                => InlineSnapshot.Validate(Tree("fun A: I32 { }"), """
                    ERROR MissingToken@[5, 5): Expected parameters ('(').


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · TypeAnnotationClause
                    · · ':'
                    · · TypeName
                    · · · IdName 'I32'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            

            [Fact]
            public void ParamList_Garbage_1()
                => InlineSnapshot.Validate(Tree("fun Foo(a @@ b) { }"), """
                    ERROR MissingToken@[9, 9): Expected a type annotation.
                    ERROR UnexpectedToken@[10, 12): Expected ',', got unknown characters.
                    ERROR MissingToken@[14, 14): Expected a type annotation.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · Garbage '@@'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_2()
                => InlineSnapshot.Validate(Tree("fun Foo(@@ a, b) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.
                    ERROR MissingToken@[12, 12): Expected a type annotation.
                    ERROR MissingToken@[15, 15): Expected a type annotation.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_3()
                => InlineSnapshot.Validate(Tree("fun Foo(@@, a, b) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.
                    ERROR MissingToken@[13, 13): Expected a type annotation.
                    ERROR MissingToken@[16, 16): Expected a type annotation.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
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
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
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
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_6()
                => InlineSnapshot.Validate(Tree("fun Foo(a @@, @@) { }"), """
                    ERROR MissingToken@[9, 9): Expected a type annotation.
                    ERROR UnexpectedToken@[10, 12): Expected ',', got unknown characters.
                    ERROR UnexpectedToken@[14, 16): Expected a parameter, got unknown characters.


                    FunDecl
                    · 'fun'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · Garbage '@@'
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_7()
                => InlineSnapshot.Validate(Tree("fun Foo(@@, a, b @@) { }"), """
                    ERROR UnexpectedToken@[8, 10): Expected a parameter, got unknown characters.
                    ERROR MissingToken@[13, 13): Expected a type annotation.
                    ERROR MissingToken@[16, 16): Expected a type annotation.
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
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'b'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
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
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ','
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · TypeName
                    · · · · · IdName
                    · · · · · · ??ID
                    · · ')'
                    · FunBody
                    · · BlockExpr '{' '}'
                    """);

            
        }
    }
}