using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public sealed class NamedCalls
    {
        [Fact]
        public void Simple1()
            => InlineSnapshot.Validate(Tree("call(name = value);"), """
                ExprStmt
                · CallExpr
                · · IdName 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · IdName 'name'
                · · · · '='
                · · · · IdName 'value'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void Simple2()
            => InlineSnapshot.Validate(Tree("call(name = val * 1 && 10 - -10);"), """
                ExprStmt
                · CallExpr
                · · IdName 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · IdName 'name'
                · · · · '='
                · · · · BinaryExpr
                · · · · · BinaryExpr
                · · · · · · IdName 'val'
                · · · · · · '*'
                · · · · · · NumberLiteral '1'
                · · · · · '&&'
                · · · · · BinaryExpr
                · · · · · · NumberLiteral '10'
                · · · · · · '-'
                · · · · · · UnaryExpr
                · · · · · · · '-'
                · · · · · · · NumberLiteral '10'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void Simple3()
            => InlineSnapshot.Validate(Tree("call(name = A().Member);"), """
                ExprStmt
                · CallExpr
                · · IdName 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · IdName 'name'
                · · · · '='
                · · · · GetMemberExpr
                · · · · · CallExpr
                · · · · · · IdName 'A'
                · · · · · · ArgList '(' ')'
                · · · · · '.'
                · · · · · IdName 'Member'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void ChainedEquals()
            => InlineSnapshot.Validate(Tree("call(name = value = chained);"), """
                ExprStmt
                · CallExpr
                · · IdName 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · IdName 'name'
                · · · · '='
                · · · · BinaryExpr
                · · · · · IdName 'value'
                · · · · · '='
                · · · · · IdName 'chained'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void ExprBeforeEqual()
            => InlineSnapshot.Validate(Tree("call(1 + 2 = value);"), """
                ExprStmt
                · CallExpr
                · · IdName 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · BinaryExpr
                · · · · · BinaryExpr
                · · · · · · NumberLiteral '1'
                · · · · · · '+'
                · · · · · · NumberLiteral '2'
                · · · · · '='
                · · · · · IdName 'value'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void ParenthesizedAssign()
            => InlineSnapshot.Validate(Tree("call((name = value));"), """
                ExprStmt
                · CallExpr
                · · IdName 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · GroupExpr
                · · · · · '('
                · · · · · BinaryExpr
                · · · · · · IdName 'name'
                · · · · · · '='
                · · · · · · IdName 'value'
                · · · · · ')'
                · · · ')'
                · ';'
                """);
    }
}