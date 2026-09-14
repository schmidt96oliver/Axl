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
                    · Path
                    · · IdName 'A'
                    · ??';'
                    """);
            
            [Fact]
            public void InFnBody()
                => InlineSnapshot.Validate(Tree("""
                                                fn A()
                                                { module Global; 1; }
                                                """), """
                    ERROR UnexpectedToken@[10, 16): Expected a statement, got 'module'.


                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · FnBody
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