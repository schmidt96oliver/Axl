using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class FnDecl
        {
            [Fact]
            public void BracedGarbageInsideBody()
                => InlineSnapshot.Validate(Tree("fn F() { (@@ {}); }"), """
                    ERROR MissingToken@[10, 10): Expected an expression.
                    ERROR UnexpectedToken@[10, 12): Expected ')', got unknown characters.


                    FnDecl
                    · 'fn'
                    · IdName 'F'
                    · ParamList '(' ')'
                    · BlockExpr
                    · · '{'
                    · · ExprStmt
                    · · · GroupExpr
                    · · · · '('
                    · · · · IdName
                    · · · · · ??ID
                    · · · · Garbage '@@' '{' '}'
                    · · · · ')'
                    · · · ';'
                    · · '}'
                    """);
            
            
            [Fact]
            public void UnclosedParamList_1()
                => InlineSnapshot.Validate(Tree("fn Foo( { }"), """
                    ERROR MissingToken@[7, 7): Expected ')'.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · ??')'
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_2()
                => InlineSnapshot.Validate(Tree("fn Foo( ;"), """
                    ERROR MissingToken@[7, 7): Expected ')'.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · ??')'
                    · BlockExpr
                    · · ??'{'
                    · · ??'}'
                    · ';'
                    """);
            [Fact]
            public void UnclosedParamList_3()
                => InlineSnapshot.Validate(Tree("fn Foo( { };"), """
                    ERROR MissingToken@[7, 7): Expected ')'.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · ??')'
                    · BlockExpr '{' '}'
                    · ';'
                    """);
            [Fact]
            public void UnclosedParamList_4()
                => InlineSnapshot.Validate(Tree("fn Foo( -> i32 { }"), """
                    ERROR MissingToken@[7, 7): Expected ')'.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · ??')'
                    · TypeAnnotationClause
                    · · '->'
                    · · NativeTypeName 'i32'
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
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
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
                    · · · TypeAnnotationClause
                    · · · · ':'
                    · · · · NativeTypeName 'i32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · TypeAnnotationClause
                    · · '->'
                    · · NativeTypeName 'i32'
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_7()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32, @@ { }"), """
                    ERROR UnexpectedToken@[15, 17): Expected a parameter, got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_8()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32, @@ -> string { }"), """
                    ERROR UnexpectedToken@[15, 17): Expected a parameter, got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · TypeAnnotationClause
                    · · '->'
                    · · NativeTypeName 'string'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void UnclosedParamList_9()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32,  => 1;"), """
                    ERROR MissingToken@[14, 14): Expected a parameter.


                    FnDecl
                    · 'fn'
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
                    · Arm
                    · · '=>'
                    · · NumberLiteral '1'
                    · ';'
                    """);
            
            [Fact]
            public void UnclosedParamList_10()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32,  = 1;"), """
                    ERROR MissingToken@[14, 14): Expected a parameter.
                    ERROR UnexpectedToken@[16, 17): Expected '=>', got '='.


                    FnDecl
                    · 'fn'
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
                    · Arm
                    · · Garbage '='
                    · · ??'=>'
                    · · NumberLiteral '1'
                    · ';'
                    """);
            
            
            [Fact]
            public void ParamList_ForgottenComma_1()
                => InlineSnapshot.Validate(Tree("fn Foo(a b) { }"), """
                    ERROR MissingToken@[8, 8): Expected ','.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_ForgottenComma_2()
                => InlineSnapshot.Validate(Tree("fn Foo(a: i32 b: string) { }"), """
                    ERROR MissingToken@[13, 13): Expected ','.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_ForgottenComma_3()
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
                    · BlockExpr '{' '}'
                    """);

            
            [Fact]
            public void Param_ForgottenId()
                => InlineSnapshot.Validate(Tree("fn A( : i32) { } "), """
                    ERROR MissingToken@[5, 5): Expected an identifier.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenTypeName()
                => InlineSnapshot.Validate(Tree("fn A(a : ) { } "), """
                    ERROR MissingToken@[8, 8): Expected a type name.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenColon_FollowedByNativeTypeName()
                => InlineSnapshot.Validate(Tree("fn A(a i32) { } "), """
                    ERROR MissingToken@[6, 6): Expected ':'.


                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName 'a'
                    · · · TypeAnnotationClause
                    · · · · ??':'
                    · · · · NativeTypeName 'i32'
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenColon_FollowedByQualifiedName_1()
                => InlineSnapshot.Validate(Tree("fn A(a a.b) { } "), """
                    ERROR MissingToken@[6, 6): Expected ':'.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_ForgottenColon_FollowedByQualifiedName_2()
                => InlineSnapshot.Validate(Tree("fn A(a a.) { } "), """
                    ERROR MissingToken@[6, 6): Expected ':'.
                    ERROR MissingToken@[9, 9): Expected an identifier.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Param_OnlyNativeTypeName()
                => InlineSnapshot.Validate(Tree("fn A(f32) { } "), """
                    ERROR MissingToken@[5, 5): Expected an identifier.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            
            [Fact]
            public void ParamList_ForgottenItem_1()
                => InlineSnapshot.Validate(Tree("fn Foo( , ) { }"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.
                    ERROR MissingToken@[9, 9): Expected a parameter.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void ParamList_ForgottenItem_2()
                => InlineSnapshot.Validate(Tree("fn Foo( , , ) { }"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.
                    ERROR MissingToken@[9, 9): Expected a parameter.
                    ERROR MissingToken@[11, 11): Expected a parameter.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void ParamList_ForgottenItem_3()
                => InlineSnapshot.Validate(Tree("fn Foo(a,) { }"), """
                    ERROR MissingToken@[9, 9): Expected a parameter.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);

            [Fact]
            public void NoParamList_1()
                => InlineSnapshot.Validate(Tree("fn A { }"), """
                    ERROR MissingToken@[4, 4): Expected parameters ('(').


                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void NoParamList_2()
                => InlineSnapshot.Validate(Tree("fn A => 1;"), """
                    ERROR MissingToken@[4, 4): Expected parameters ('(').


                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · Arm
                    · · '=>'
                    · · NumberLiteral '1'
                    · ';'
                    """);
            
            [Fact]
            public void NoParamList_3()
                => InlineSnapshot.Validate(Tree("fn A -> i32 { }"), """
                    ERROR MissingToken@[4, 4): Expected parameters ('(').


                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · TypeAnnotationClause
                    · · '->'
                    · · NativeTypeName 'i32'
                    · BlockExpr '{' '}'
                    """);
            
            

            [Fact]
            public void ParamList_Garbage_1()
                => InlineSnapshot.Validate(Tree("fn Foo(a @@ b) { }"), """
                    ERROR UnexpectedToken@[9, 11): Expected ',', got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_2()
                => InlineSnapshot.Validate(Tree("fn Foo(@@ a, b) { }"), """
                    ERROR UnexpectedToken@[7, 9): Expected a parameter, got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_3()
                => InlineSnapshot.Validate(Tree("fn Foo(@@, a, b) { }"), """
                    ERROR UnexpectedToken@[7, 9): Expected a parameter, got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_4()
                => InlineSnapshot.Validate(Tree("fn Foo(@@) { }"), """
                    ERROR UnexpectedToken@[7, 9): Expected a parameter, got unknown characters.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Garbage '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_5()
                => InlineSnapshot.Validate(Tree("fn Foo(@@, ) { }"), """
                    ERROR UnexpectedToken@[7, 9): Expected a parameter, got unknown characters.
                    ERROR MissingToken@[10, 10): Expected a parameter.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_6()
                => InlineSnapshot.Validate(Tree("fn Foo(a @@, @@) { }"), """
                    ERROR UnexpectedToken@[9, 11): Expected ',', got unknown characters.
                    ERROR UnexpectedToken@[13, 15): Expected a parameter, got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_7()
                => InlineSnapshot.Validate(Tree("fn Foo(@@, a, b @@) { }"), """
                    ERROR UnexpectedToken@[7, 9): Expected a parameter, got unknown characters.
                    ERROR UnexpectedToken@[16, 18): Expected ')', got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void ParamList_Garbage_8()
                => InlineSnapshot.Validate(Tree("fn Foo(@@,@@) { }"), """
                    ERROR UnexpectedToken@[7, 9): Expected a parameter, got unknown characters.
                    ERROR UnexpectedToken@[10, 12): Expected a parameter, got unknown characters.


                    FnDecl
                    · 'fn'
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
                    · BlockExpr '{' '}'
                    """);

            
            [Fact]
            public void NativeNotDelimited_LeavesRestAlone_1()
                => InlineSnapshot.Validate(Tree("""
                                                native
                                                module AB
                                                {
                                                    
                                                }
                                                """), """
                    ERROR MissingToken@[6, 6): Expected '('.


                    Garbage
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    ModuleDecl
                    · 'module'
                    · Path
                    · · IdName 'AB'
                    · '{'
                    · '}'
                    """);
            [Fact]
            public void NativeNotDelimited_LeavesRestAlone_2()
                => InlineSnapshot.Validate(Tree("""
                                                native
                                                1 + 2;
                                                """), """
                    ERROR MissingToken@[6, 6): Expected '('.


                    Garbage
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    ExprStmt
                    · BinaryExpr
                    · · NumberLiteral '1'
                    · · '+'
                    · · NumberLiteral '2'
                    · ';'
                    """);
            [Fact]
            public void NativeNotDelimited_LeavesRestAlone_3()
                => InlineSnapshot.Validate(Tree("""
                                                native
                                                var a = 2;
                                                """), """
                    ERROR MissingToken@[6, 6): Expected '('.


                    Garbage
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    VarDecl
                    · 'var'
                    · IdName 'a'
                    · InitializerClause
                    · · '='
                    · · NumberLiteral '2'
                    · ';'
                    """);
            
            [Fact]
            public void NativeNotDelimited_LeavesRestAlone_4()
                => InlineSnapshot.Validate(Tree("""
                                                public native
                                                var a = 2;
                                                """), """
                    ERROR MissingToken@[13, 13): Expected '('.


                    Garbage
                    · 'public'
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    VarDecl
                    · 'var'
                    · IdName 'a'
                    · InitializerClause
                    · · '='
                    · · NumberLiteral '2'
                    · ';'
                    """);
            
            
            [Fact]
            public void Native_1()
                => InlineSnapshot.Validate(Tree("""
                                                native;
                                                fn A() { }
                                                """), """
                    ERROR MissingToken@[6, 6): Expected '('.


                    Garbage
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    · ';'
                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void Native_2()
                => InlineSnapshot.Validate(Tree("""
                                                native fn ;
                                                fn A() { }
                                                """), """
                    ERROR MissingToken@[6, 6): Expected '('.
                    ERROR MissingToken@[9, 9): Expected an identifier.


                    FnDecl
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    · 'fn'
                    · IdName
                    · · ??ID
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · ';'
                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void Native_3()
                => InlineSnapshot.Validate(Tree("native fn A();"), """
                    ERROR MissingToken@[6, 6): Expected '('.


                    FnDecl
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · ';'
                    """);
            [Fact]
            public void Native_4()
                => InlineSnapshot.Validate(Tree("native( fn A();"), """
                    ERROR MissingToken@[7, 7): Expected a string.


                    FnDecl
                    · NativeClause
                    · · 'native'
                    · · '('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · ';'
                    """);
            
            [Fact]
            public void Native_5()
                => InlineSnapshot.Validate(Tree("native(\"Foo\" fn A();"), """
                    ERROR MissingToken@[12, 12): Expected ')'.


                    FnDecl
                    · NativeClause
                    · · 'native'
                    · · '('
                    · · StringExpr
                    · · · '"'
                    · · · StringText 'Foo'
                    · · · '"'
                    · · ??')'
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · ';'
                    """);
            [Fact]
            public void Native_6()
                => InlineSnapshot.Validate(Tree("native() fn A();"), """
                    ERROR MissingToken@[7, 7): Expected a string.


                    FnDecl
                    · NativeClause
                    · · 'native'
                    · · '('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ')'
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · ';'
                    """);
            [Fact]
            public void Native_7()
                => InlineSnapshot.Validate(Tree("public native ;"), """
                    ERROR MissingToken@[13, 13): Expected '('.


                    Garbage
                    · 'public'
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    · ';'
                    """);
            [Fact]
            public void Native_8()
                => InlineSnapshot.Validate(Tree("public public private native fn;"), """
                    ERROR MissingToken@[28, 28): Expected '('.
                    ERROR MissingToken@[31, 31): Expected an identifier.


                    FnDecl
                    · 'public'
                    · 'public'
                    · 'private'
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    · 'fn'
                    · IdName
                    · · ??ID
                    · ParamList
                    · · ??'('
                    · · ??')'
                    · ';'
                    """);
        }
    }
}