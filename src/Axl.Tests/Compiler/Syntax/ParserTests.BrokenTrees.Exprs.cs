using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class Exprs
        {
            [Fact]
            public void If_MissingCondition_1()
            
                => InlineSnapshot.Validate(Tree("if "), """
                    ERROR MissingToken@[2, 2): Expected '('.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · ??'('
                    · · · NameRef
                    · · · · ??ID
                    · · · ??')'
                    · · NameRef
                    · · · ??ID
                    · ??';'
                    """);
            [Fact]
            public void If_MissingCondition_2()
                => InlineSnapshot.Validate(Tree("if 1"), """
                    ERROR MissingToken@[2, 2): Expected '('.
                    ERROR MissingToken@[4, 4): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · ??'('
                    · · · NumberLiteral '1'
                    · · · ??')'
                    · · NameRef
                    · · · ??ID
                    · ??';'
                    """);
            [Fact]
            public void If_MissingCondition_3()
                => InlineSnapshot.Validate(Tree("if 1 2;"), """
                    ERROR MissingToken@[2, 2): Expected '('.
                    ERROR MissingToken@[4, 4): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · ??'('
                    · · · NumberLiteral '1'
                    · · · ??')'
                    · · NumberLiteral '2'
                    · ';'
                    """);
            
            [Fact]
            public void If_UnclosedCondition_1()
                => InlineSnapshot.Validate(Tree("""
                                                if (a > b 
                                                { inner; }
                                                """
                ), """
                    ERROR MissingToken@[9, 9): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · NameRef 'a'
                    · · · · '>'
                    · · · · NameRef 'b'
                    · · · ??')'
                    · · BlockExpr
                    · · · '{'
                    · · · ExprStmt
                    · · · · NameRef 'inner'
                    · · · · ';'
                    · · · '}'
                    """);
            
            [Fact]
            public void If_UnclosedCondition_2()
                => InlineSnapshot.Validate(Tree("""
                                                if (a > b 
                                                    inner;
                                                """
                ), """
                    ERROR MissingToken@[9, 9): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · NameRef 'a'
                    · · · · '>'
                    · · · · NameRef 'b'
                    · · · ??')'
                    · · NameRef 'inner'
                    · ';'
                    """);
            
            [Fact]
            public void If_UnclosedCondition_3()
                => InlineSnapshot.Validate(Tree("""
                                                if (a > b 
                                                else inner;  
                                                """
                ), """
                    ERROR MissingToken@[9, 9): Expected ')'.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · NameRef 'a'
                    · · · · '>'
                    · · · · NameRef 'b'
                    · · · ??')'
                    · · NameRef
                    · · · ??ID
                    · · ElseClause
                    · · · 'else'
                    · · · NameRef 'inner'
                    · ';'
                    """);
        }
    }
}