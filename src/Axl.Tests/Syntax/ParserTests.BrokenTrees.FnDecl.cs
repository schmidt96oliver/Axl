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
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · BlockExpr '{' '}'
                    """);
            [Fact]
            public void UnclosedParamList_2()
                => InlineSnapshot.Validate(Tree("fn Foo( ;"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · BlockExpr
                    · · ??'{'
                    · · ??'}'
                    · ';'
                    """);
            [Fact]
            public void UnclosedParamList_3()
                => InlineSnapshot.Validate(Tree("fn Foo( { };"), """
                    ERROR MissingToken@[7, 7): Expected a parameter.


                    FnDecl
                    · 'fn'
                    · IdName 'Foo'
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
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
                    · ParamList
                    · · '('
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
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
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ','
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · '->'
                    · NativeTypeName 'i32'
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
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ','
                    · · Error '@@'
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
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ','
                    · · Error '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ??')'
                    · '->'
                    · NativeTypeName 'string'
                    · BlockExpr '{' '}'
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
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · · ':'
                    · · · NativeTypeName 'string'
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
                    · · · ':'
                    · · · NativeTypeName 'i32'
                    · · ??','
                    · · Param
                    · · · IdName 'b'
                    · · ??','
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
                    · '->'
                    · NativeTypeName 'i32'
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
                    · · Error '@@'
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
                    · · Error '@@'
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
                    · · Error '@@'
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
                    · · Error '@@'
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
                    · · Error '@@'
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
                    · · Error '@@'
                    · · ','
                    · · Error '@@'
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
                    · · Error '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Param
                    · · · IdName 'a'
                    · · ','
                    · · Param
                    · · · IdName 'b'
                    · · Error '@@'
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
                    · · Error '@@'
                    · · Param
                    · · · IdName
                    · · · · ??ID
                    · · ','
                    · · Error '@@'
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


                    Error
                    · NativeClause
                    · · 'native'
                    · · ??'('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    ModuleDecl
                    · 'module'
                    · QualifiedName
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


                    Error
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


                    Error
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
                    · '='
                    · NumberLiteral '2'
                    · ';'
                    """);
            
            
            [Fact]
            public void Native_1()
                => InlineSnapshot.Validate(Tree("""
                                                native;
                                                fn A() { }
                                                """), """
                    ERROR MissingToken@[6, 6): Expected '('.


                    Error
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
                => InlineSnapshot.Validate(Tree("native(a fn A();"), """
                    ERROR MissingToken@[7, 7): Expected a string.
                    ERROR MissingToken@[8, 8): Expected ';'.
                    ERROR MissingToken@[15, 15): Expected a body.


                    Error
                    · NativeClause
                    · · 'native'
                    · · '('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    ExprStmt
                    · IdName 'a'
                    · ??';'
                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · BlockExpr
                    · · ??'{'
                    · · ??'}'
                    · ';'
                    """);
            [Fact]
            public void Native_6()
                => InlineSnapshot.Validate(Tree("native(a) fn A();"), """
                    ERROR MissingToken@[7, 7): Expected a string.
                    ERROR MissingToken@[8, 8): Expected ';'.
                    ERROR MissingToken@[16, 16): Expected a body.


                    Error
                    · NativeClause
                    · · 'native'
                    · · '('
                    · · StringExpr
                    · · · ??'"'
                    · · · ??'"'
                    · · ??')'
                    ExprStmt
                    · IdName 'a'
                    · ??';'
                    Error ')'
                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · BlockExpr
                    · · ??'{'
                    · · ??'}'
                    · ';'
                    """);
            
            [Fact]
            public void Native_7()
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
            public void Native_8()
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
        }
    }
}