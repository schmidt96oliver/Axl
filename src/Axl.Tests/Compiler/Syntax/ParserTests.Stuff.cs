using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    public sealed class Stuff
    {
        [Fact]
        public void Mut_Arg1()
            => InlineSnapshot.Validate(Tree("A(mut a);"), """
                ExprStmt
                · CallExpr
                · · NameRef 'A'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · 'mut'
                · · · · NameRef 'a'
                · · · ')'
                · ';'
                """);
        [Fact]
        public void Mut_Arg2()
            => InlineSnapshot.Validate(Tree("A(mut a, mut b);"), """
                ExprStmt
                · CallExpr
                · · NameRef 'A'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · 'mut'
                · · · · NameRef 'a'
                · · · ','
                · · · Arg
                · · · · 'mut'
                · · · · NameRef 'b'
                · · · ')'
                · ';'
                """);
        [Fact]
        public void Mut_Arg3()
            => InlineSnapshot.Validate(Tree("A(mut mut a);"), """
                ERROR MissingToken@[5, 5): Expected an expression.


                ExprStmt
                · CallExpr
                · · NameRef 'A'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · 'mut'
                · · · · NameRef
                · · · · · ??ID
                · · · ??','
                · · · Arg
                · · · · 'mut'
                · · · · NameRef 'a'
                · · · ')'
                · ';'
                """);

        [Fact]
        public void Mut_Param()
            => InlineSnapshot.Validate(Tree("fun A(mut a: I32);"), """
                FunDecl
                · 'fun'
                · FunName 'A'
                · ParamList
                · · '('
                · · Param
                · · · 'mut'
                · · · 'a'
                · · · TypeAnnotationClause
                · · · · ':'
                · · · · NameRef 'I32'
                · · ')'
                · FunBody ';'
                """);
        [Fact]
        public void Mut_Return()
            => InlineSnapshot.Validate(Tree("return mut a;"), """
                ExprStmt
                · ReturnExpr
                · · 'return'
                · · 'mut'
                · · NameRef 'a'
                · ';'
                """);
        [Fact]
        public void Mut_ExprBody()
            => InlineSnapshot.Validate(Tree("fun A() = mut a;"), """
                FunDecl
                · 'fun'
                · FunName 'A'
                · ParamList '(' ')'
                · FunBody
                · · '='
                · · 'mut'
                · · NameRef 'a'
                · · ';'
                """);

        [Fact]
        public void Subscript_1()
            => InlineSnapshot.Validate(Tree("f[a, b]();"), """
                ExprStmt
                · CallExpr
                · · SubscriptExpr
                · · · NameRef 'f'
                · · · SubscriptArgList
                · · · · '['
                · · · · NameRef 'a'
                · · · · ','
                · · · · NameRef 'b'
                · · · · ']'
                · · ArgList '(' ')'
                · ';'
                """);
        [Fact]
        public void Subscript_2()
            => InlineSnapshot.Validate(Tree("f[a + b, a()[4]()]()[a];"), """
                ExprStmt
                · SubscriptExpr
                · · CallExpr
                · · · SubscriptExpr
                · · · · NameRef 'f'
                · · · · SubscriptArgList
                · · · · · '['
                · · · · · BinaryExpr
                · · · · · · NameRef 'a'
                · · · · · · '+'
                · · · · · · NameRef 'b'
                · · · · · ','
                · · · · · CallExpr
                · · · · · · SubscriptExpr
                · · · · · · · CallExpr
                · · · · · · · · NameRef 'a'
                · · · · · · · · ArgList '(' ')'
                · · · · · · · SubscriptArgList
                · · · · · · · · '['
                · · · · · · · · NumberLiteral '4'
                · · · · · · · · ']'
                · · · · · · ArgList '(' ')'
                · · · · · ']'
                · · · ArgList '(' ')'
                · · SubscriptArgList
                · · · '['
                · · · NameRef 'a'
                · · · ']'
                · ';'
                """);

        [Fact]
        public void TypeName_1()
            => InlineSnapshot.Validate(Tree("let a: A[];"), """
                ERROR MissingToken@[9, 9): Expected a type name.


                VarDecl
                · 'let'
                · 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · NameRef 'A'
                · · · SubscriptArgList
                · · · · '['
                · · · · NameRef
                · · · · · ??ID
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_2()
            => InlineSnapshot.Validate(Tree("let a: A[T1, T2];"), """
                VarDecl
                · 'let'
                · 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · NameRef 'A'
                · · · SubscriptArgList
                · · · · '['
                · · · · NameRef 'T1'
                · · · · ','
                · · · · NameRef 'T2'
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_3()
            => InlineSnapshot.Validate(Tree("let a: A.B[T1, T2];"), """
                VarDecl
                · 'let'
                · 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · MemberAccessExpr
                · · · · NameRef 'A'
                · · · · '.'
                · · · · NameRef 'B'
                · · · SubscriptArgList
                · · · · '['
                · · · · NameRef 'T1'
                · · · · ','
                · · · · NameRef 'T2'
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_4()
            => InlineSnapshot.Validate(Tree("let a: A[T].B[T1, T2];"), """
                VarDecl
                · 'let'
                · 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · MemberAccessExpr
                · · · · SubscriptExpr
                · · · · · NameRef 'A'
                · · · · · SubscriptArgList
                · · · · · · '['
                · · · · · · NameRef 'T'
                · · · · · · ']'
                · · · · '.'
                · · · · NameRef 'B'
                · · · SubscriptArgList
                · · · · '['
                · · · · NameRef 'T1'
                · · · · ','
                · · · · NameRef 'T2'
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_5()
            => InlineSnapshot.Validate(Tree("let a: A[T].B[U].C[W];"), """
                VarDecl
                · 'let'
                · 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · MemberAccessExpr
                · · · · SubscriptExpr
                · · · · · MemberAccessExpr
                · · · · · · SubscriptExpr
                · · · · · · · NameRef 'A'
                · · · · · · · SubscriptArgList
                · · · · · · · · '['
                · · · · · · · · NameRef 'T'
                · · · · · · · · ']'
                · · · · · · '.'
                · · · · · · NameRef 'B'
                · · · · · SubscriptArgList
                · · · · · · '['
                · · · · · · NameRef 'U'
                · · · · · · ']'
                · · · · '.'
                · · · · NameRef 'C'
                · · · SubscriptArgList
                · · · · '['
                · · · · NameRef 'W'
                · · · · ']'
                · ';'
                """);
    }
}