using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class Exprs
        {
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
                    · · · · IdName 'a'
                    · · · · '>'
                    · · · · IdName 'b'
                    · · · ??')'
                    · · IdName 'inner'
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
                    · · · · IdName 'a'
                    · · · · '>'
                    · · · · IdName 'b'
                    · · · ??')'
                    · · IdName
                    · · · ??ID
                    · · ElseClause
                    · · · 'else'
                    · · · IdName 'inner'
                    · ';'
                    """);


            [Fact]
            public void If_EqualInsteadOfDoubleEqual_1()
                => InlineSnapshot.Validate(Tree("if (a = 1) { }"), """
                    ERROR UnexpectedToken@[6, 7): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · Garbage '='
                    · · · · ??'=='
                    · · · · NumberLiteral '1'
                    · · · ')'
                    · · BlockExpr '{' '}'
                    """);
                    
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_2()
                => InlineSnapshot.Validate(Tree("if (a = 1) 1;"), """
                    ERROR UnexpectedToken@[6, 7): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · Garbage '='
                    · · · · ??'=='
                    · · · · NumberLiteral '1'
                    · · · ')'
                    · · NumberLiteral '1'
                    · ';'
                    """);
                    
            
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_3()
                => InlineSnapshot.Validate(Tree("if (a = ) { }"), """
                    ERROR UnexpectedToken@[6, 7): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · Garbage '='
                    · · · · ??'=='
                    · · · · IdName
                    · · · · · ??ID
                    · · · ')'
                    · · BlockExpr '{' '}'
                    """);
            
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_4()
                => InlineSnapshot.Validate(Tree("if (a = 1 == 2) true;"), """
                    ERROR UnexpectedToken@[6, 7): Expected '==', got '='.
                    ERROR InvalidOperatorChaining@[6, 7), [10, 12): Cannot chain '=' and '=='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · ErrorExpr
                    · · · · IdName 'a'
                    · · · · Garbage '='
                    · · · · ??'=='
                    · · · · NumberLiteral '1'
                    · · · · '=='
                    · · · · NumberLiteral '2'
                    · · · ')'
                    · · TrueLiteral 'true'
                    · ';'
                    """);
            
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_5()
                => InlineSnapshot.Validate(Tree("if (a = 1 and b == 2) true;"), """
                    ERROR UnexpectedToken@[6, 7): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · BinaryExpr
                    · · · · · IdName 'a'
                    · · · · · Garbage '='
                    · · · · · ??'=='
                    · · · · · NumberLiteral '1'
                    · · · · 'and'
                    · · · · BinaryExpr
                    · · · · · IdName 'b'
                    · · · · · '=='
                    · · · · · NumberLiteral '2'
                    · · · ')'
                    · · TrueLiteral 'true'
                    · ';'
                    """);
            
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_6()
                => InlineSnapshot.Validate(Tree("if (a = 1 + 2 * 3) true;"), """
                    ERROR UnexpectedToken@[6, 7): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · ConditionClause
                    · · · '('
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · Garbage '='
                    · · · · ??'=='
                    · · · · BinaryExpr
                    · · · · · NumberLiteral '1'
                    · · · · · '+'
                    · · · · · BinaryExpr
                    · · · · · · NumberLiteral '2'
                    · · · · · · '*'
                    · · · · · · NumberLiteral '3'
                    · · · ')'
                    · · TrueLiteral 'true'
                    · ';'
                    """);
        }
    }
}