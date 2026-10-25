using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

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
    }
}