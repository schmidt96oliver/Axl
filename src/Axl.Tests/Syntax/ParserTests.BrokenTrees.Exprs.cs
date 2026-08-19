using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class Exprs
        {
            [Fact]
            public void If_UnclosedGroupPredicate_1()
                => InlineSnapshot.Validate(Tree("""
                                                if (a > b 
                                                { inner; }
                                                """
                ), """
                    ERROR MissingToken@[9, 9): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · GroupExpr
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · '>'
                    · · · · IdName 'b'
                    · · · ??')'
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · IdName 'inner'
                    · · · · ';'
                    · · · '}'
                    """);
            
            [Fact]
            public void If_UnclosedGroupPredicate_2()
                => InlineSnapshot.Validate(Tree("""
                                                if (a > b 
                                                    => inner;
                                                """
                ), """
                    ERROR MissingToken@[9, 9): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · GroupExpr
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · '>'
                    · · · · IdName 'b'
                    · · · ??')'
                    · · Arm
                    · · · '=>'
                    · · · IdName 'inner'
                    · ';'
                    """);
            
            [Fact]
            public void If_UnclosedGroupPredicate_3()
                => InlineSnapshot.Validate(Tree("""
                                                if (a > b 
                                                else => inner;  
                                                """
                ), """
                    ERROR MissingToken@[9, 9): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · GroupExpr
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · '>'
                    · · · · IdName 'b'
                    · · · ??')'
                    · · BlockExpr
                    · · · ??'{'
                    · · · ??'}'
                    · · 'else'
                    · · Arm
                    · · · '=>'
                    · · · IdName 'inner'
                    · ';'
                    """);
        }
    }
}