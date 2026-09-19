using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class ModuleDecl
        {
            [Fact]
            public void MissingSemicolon()
                => InlineSnapshot.Validate(Tree("module A"), """
                    ERROR MissingToken@[8, 8): Expected ';'.


                    ModuleDecl
                    · 'module'
                    · TypeName
                    · · IdName 'A'
                    · ??';'
                    """);
            
            [Fact]
            public void InFunBody()
                => InlineSnapshot.Validate(Tree("""
                                                fun A()
                                                { module Global; 1; }
                                                """), """
                    ERROR UnexpectedToken@[11, 17): Expected a statement, got 'module'.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · Garbage 'module'
                    · · · ExprStmt
                    · · · · IdName 'Global'
                    · · · · ';'
                    · · · ExprStmt
                    · · · · NumberLiteral '1'
                    · · · · ';'
                    · · · '}'
                    """);
            [Fact]
            public void InBlock()
                => InlineSnapshot.Validate(Tree("{ module Global; }"), """
                    ERROR UnexpectedToken@[2, 8): Expected a statement, got 'module'.


                    ExprStmt
                    · BlockExpr
                    · · '{'
                    · · Garbage 'module'
                    · · ExprStmt
                    · · · IdName 'Global'
                    · · · ';'
                    · · '}'
                    """);
        }
    }
}