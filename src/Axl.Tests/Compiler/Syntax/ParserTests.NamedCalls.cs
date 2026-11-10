using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    public sealed class NamedCalls
    {
        [Fact]
        public void Simple1()
            => InlineSnapshot.Validate(Tree("call(name = value);"), """
                ExprStmt
                · CallExpr
                · · NameRef 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · NameRef 'name'
                · · · · '='
                · · · · NameRef 'value'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void Simple2()
            => InlineSnapshot.Validate(Tree("call(name = val * 1 && 10 - -10);"), """
                ExprStmt
                · CallExpr
                · · NameRef 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · NameRef 'name'
                · · · · '='
                · · · · BinaryExpr
                · · · · · BinaryExpr
                · · · · · · NameRef 'val'
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
                · · NameRef 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · NameRef 'name'
                · · · · '='
                · · · · MemberAccessExpr
                · · · · · CallExpr
                · · · · · · NameRef 'A'
                · · · · · · ArgList '(' ')'
                · · · · · '.'
                · · · · · NameRef 'Member'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void ChainedEquals()
            => InlineSnapshot.Validate(Tree("call(name = value = chained);"), """
                ExprStmt
                · CallExpr
                · · NameRef 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · NameRef 'name'
                · · · · '='
                · · · · BinaryExpr
                · · · · · NameRef 'value'
                · · · · · '='
                · · · · · NameRef 'chained'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void ExprBeforeEqual()
            => InlineSnapshot.Validate(Tree("call(1 + 2 = value);"), """
                ExprStmt
                · CallExpr
                · · NameRef 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · BinaryExpr
                · · · · · BinaryExpr
                · · · · · · NumberLiteral '1'
                · · · · · · '+'
                · · · · · · NumberLiteral '2'
                · · · · · '='
                · · · · · NameRef 'value'
                · · · ')'
                · ';'
                """);
        
        [Fact]
        public void ParenthesizedAssign()
            => InlineSnapshot.Validate(Tree("call((name = value));"), """
                ExprStmt
                · CallExpr
                · · NameRef 'call'
                · · ArgList
                · · · '('
                · · · Arg
                · · · · GroupExpr
                · · · · · '('
                · · · · · BinaryExpr
                · · · · · · NameRef 'name'
                · · · · · · '='
                · · · · · · NameRef 'value'
                · · · · · ')'
                · · · ')'
                · ';'
                """);
    }
}