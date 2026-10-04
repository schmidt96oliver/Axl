using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class NamespaceDecl
        {
            [Fact]
            public void MissingSemicolon()
                => InlineSnapshot.Validate(Tree("namespace A"), """
                    ERROR MissingToken@[11, 11): Expected ';'.


                    NamespaceDecl
                    · 'namespace'
                    · IdName 'A'
                    · ??';'
                    """);
            
            [Fact]
            public void InFunBody()
                => InlineSnapshot.Validate(Tree("""
                                                fun A()
                                                { namespace Global; 1; }
                                                """), """
                    ERROR UnexpectedToken@[11, 20): Expected a statement, got 'namespace'.


                    FunDecl
                    · 'fun'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · FunBody
                    · · BlockExpr
                    · · · '{'
                    · · · Garbage 'namespace'
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
                => InlineSnapshot.Validate(Tree("{ namespace Global; }"), """
                    ERROR UnexpectedToken@[2, 11): Expected a statement, got 'namespace'.


                    ExprStmt
                    · BlockExpr
                    · · '{'
                    · · Garbage 'namespace'
                    · · ExprStmt
                    · · · IdName 'Global'
                    · · · ';'
                    · · '}'
                    """);
        }
    }
}