using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class Stmts
        {
            [Fact]
            public void StraySemicolon_Global()
                => InlineSnapshot.Validate(Tree(";"), """
                    ERROR UnexpectedToken@[0, 1): Expected a statement, got ';'.


                    Error ';'
                    """);
            
            [Fact]
            public void StraySemicolon_InBlock()
                => InlineSnapshot.Validate(Tree("{ ; }"), """
                    ERROR UnexpectedToken@[2, 3): Expected a statement, got ';'.


                    ExprStmt
                    · BlockExpr
                    · · '{'
                    · · Error ';'
                    · · '}'
                    """);
            
            [Fact]
            public void StraySemicolon_AfterBlockArm()
                => InlineSnapshot.Validate(Tree("{ => 1; }"), """
                    ERROR UnexpectedToken@[6, 7): Expected '}', got ';'.


                    ExprStmt
                    · BlockExpr
                    · · '{'
                    · · Arm
                    · · · '=>'
                    · · · NumberLiteral '1'
                    · · Error ';'
                    · · '}'
                    """);
        }
    }
}