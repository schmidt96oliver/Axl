using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    public sealed class Stuff
    {
        [Fact]
        public void MutExpr_1()
            => InlineSnapshot.Validate(Tree("mut 1 + 2;"), """
                ExprStmt
                · MutExpr
                · · 'mut'
                · · BinaryExpr
                · · · NumberLiteral '1'
                · · · '+'
                · · · NumberLiteral '2'
                · ';'
                """);
        [Fact]
        public void MutExpr_2()
            => InlineSnapshot.Validate(Tree("A(mut a);"), """
                ExprStmt
                · CallExpr
                · · IdName 'A'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · MutExpr
                · · · · · 'mut'
                · · · · · IdName 'a'
                · · · ')'
                · ';'
                """);

        [Fact]
        public void Subscript_1()
            => InlineSnapshot.Validate(Tree("f[a, b]();"), """
                ExprStmt
                · CallExpr
                · · SubscriptExpr
                · · · IdName 'f'
                · · · SubscriptArgList
                · · · · '['
                · · · · IdName 'a'
                · · · · ','
                · · · · IdName 'b'
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
                · · · · IdName 'f'
                · · · · SubscriptArgList
                · · · · · '['
                · · · · · BinaryExpr
                · · · · · · IdName 'a'
                · · · · · · '+'
                · · · · · · IdName 'b'
                · · · · · ','
                · · · · · CallExpr
                · · · · · · SubscriptExpr
                · · · · · · · CallExpr
                · · · · · · · · IdName 'a'
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
                · · · IdName 'a'
                · · · ']'
                · ';'
                """);

        [Fact]
        public void TypeName_1()
            => InlineSnapshot.Validate(Tree("let a: A[];"), """
                ERROR MissingToken@[9, 9): Expected a type name.


                VarDecl
                · 'let'
                · IdName 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · IdName 'A'
                · · · SubscriptArgList
                · · · · '['
                · · · · IdName
                · · · · · ??ID
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_2()
            => InlineSnapshot.Validate(Tree("let a: A[T1, T2];"), """
                VarDecl
                · 'let'
                · IdName 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · IdName 'A'
                · · · SubscriptArgList
                · · · · '['
                · · · · IdName 'T1'
                · · · · ','
                · · · · IdName 'T2'
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_3()
            => InlineSnapshot.Validate(Tree("let a: A.B[T1, T2];"), """
                VarDecl
                · 'let'
                · IdName 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · MemberAccessExpr
                · · · · IdName 'A'
                · · · · '.'
                · · · · IdName 'B'
                · · · SubscriptArgList
                · · · · '['
                · · · · IdName 'T1'
                · · · · ','
                · · · · IdName 'T2'
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_4()
            => InlineSnapshot.Validate(Tree("let a: A[T].B[T1, T2];"), """
                VarDecl
                · 'let'
                · IdName 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · MemberAccessExpr
                · · · · SubscriptExpr
                · · · · · IdName 'A'
                · · · · · SubscriptArgList
                · · · · · · '['
                · · · · · · IdName 'T'
                · · · · · · ']'
                · · · · '.'
                · · · · IdName 'B'
                · · · SubscriptArgList
                · · · · '['
                · · · · IdName 'T1'
                · · · · ','
                · · · · IdName 'T2'
                · · · · ']'
                · ';'
                """);
        [Fact]
        public void TypeName_5()
            => InlineSnapshot.Validate(Tree("let a: A[T].B[U].C[W];"), """
                VarDecl
                · 'let'
                · IdName 'a'
                · TypeAnnotationClause
                · · ':'
                · · SubscriptExpr
                · · · MemberAccessExpr
                · · · · SubscriptExpr
                · · · · · MemberAccessExpr
                · · · · · · SubscriptExpr
                · · · · · · · IdName 'A'
                · · · · · · · SubscriptArgList
                · · · · · · · · '['
                · · · · · · · · IdName 'T'
                · · · · · · · · ']'
                · · · · · · '.'
                · · · · · · IdName 'B'
                · · · · · SubscriptArgList
                · · · · · · '['
                · · · · · · IdName 'U'
                · · · · · · ']'
                · · · · '.'
                · · · · IdName 'C'
                · · · SubscriptArgList
                · · · · '['
                · · · · IdName 'W'
                · · · · ']'
                · ';'
                """);
    }
}