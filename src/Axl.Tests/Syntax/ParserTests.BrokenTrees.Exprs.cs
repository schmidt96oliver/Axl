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


            [Fact]
            public void If_EqualInsteadOfDoubleEqual_1()
            // Expect `=` as `=>`
                => InlineSnapshot.Validate(Tree("if true = break;"), """
                    ERROR UnexpectedToken@[8, 9): Expected '=>', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · TrueLiteral 'true'
                    · · Arm
                    · · · Error '='
                    · · · ??'=>'
                    · · · BreakExpr 'break'
                    · ';'
                    """);

            [Fact]
            public void If_EqualInsteadOfDoubleEqual_2()
            // Expect `=` as `==`
                => InlineSnapshot.Validate(Tree("if a = 1;"), """
                    ERROR UnexpectedToken@[5, 6): Expected '==', got '='.
                    ERROR MissingToken@[8, 8): Expected a body.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · BinaryExpr
                    · · · IdName 'a'
                    · · · Error '='
                    · · · ??'=='
                    · · · NumberLiteral '1'
                    · · BlockExpr
                    · · · ??'{'
                    · · · ??'}'
                    · ';'
                    """);
                    
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_3()
            // Expect `=` as `==`
                => InlineSnapshot.Validate(Tree("if a = 1 => 1;"), """
                    ERROR UnexpectedToken@[5, 6): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · BinaryExpr
                    · · · IdName 'a'
                    · · · Error '='
                    · · · ??'=='
                    · · · NumberLiteral '1'
                    · · Arm
                    · · · '=>'
                    · · · NumberLiteral '1'
                    · ';'
                    """);
                    
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_4()
            // Expect 1. `=` as `==`, 2. `=` as `=>`
                => InlineSnapshot.Validate(Tree("if a = 1 = true;"), """
                    ERROR UnexpectedToken@[5, 6): Expected '==', got '='.
                    ERROR UnexpectedToken@[9, 10): Expected '=>', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · BinaryExpr
                    · · · IdName 'a'
                    · · · Error '='
                    · · · ??'=='
                    · · · NumberLiteral '1'
                    · · Arm
                    · · · Error '='
                    · · · ??'=>'
                    · · · TrueLiteral 'true'
                    · ';'
                    """);
                    
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_5()
            // Expect `=` as `=>`
                => InlineSnapshot.Validate(Tree("if a = { 1; }"), """
                    ERROR UnexpectedToken@[5, 6): Expected '=>', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · IdName 'a'
                    · · Arm
                    · · · Error '='
                    · · · ??'=>'
                    · · · BlockExpr
                    · · · · '{'
                    · · · · ExprStmt
                    · · · · · NumberLiteral '1'
                    · · · · · ';'
                    · · · · '}'
                    """);
            
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_6()
            // Expect `=` as `==`
                => InlineSnapshot.Validate(Tree("if a = 1 == 2 => true;"), """
                    ERROR UnexpectedToken@[5, 6): Expected '==', got '='.
                    ERROR InvalidOperatorChaining@[5, 6), [9, 11): Cannot chain '=' and '=='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · Error
                    · · · IdName 'a'
                    · · · Error '='
                    · · · ??'=='
                    · · · NumberLiteral '1'
                    · · · '=='
                    · · · NumberLiteral '2'
                    · · Arm
                    · · · '=>'
                    · · · TrueLiteral 'true'
                    · ';'
                    """);
            
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_7()
            // Expect `=` as `==`
                => InlineSnapshot.Validate(Tree("if a = 1 and b == 2 => true;"), """
                    ERROR UnexpectedToken@[5, 6): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · BinaryExpr
                    · · · BinaryExpr
                    · · · · IdName 'a'
                    · · · · Error '='
                    · · · · ??'=='
                    · · · · NumberLiteral '1'
                    · · · 'and'
                    · · · BinaryExpr
                    · · · · IdName 'b'
                    · · · · '=='
                    · · · · NumberLiteral '2'
                    · · Arm
                    · · · '=>'
                    · · · TrueLiteral 'true'
                    · ';'
                    """);
            
            [Fact]
            public void If_EqualInsteadOfDoubleEqual_8()
            // Expect `=` as `==`
                => InlineSnapshot.Validate(Tree("if a = 1 + 2 * 3 => true;"), """
                    ERROR UnexpectedToken@[5, 6): Expected '==', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · BinaryExpr
                    · · · IdName 'a'
                    · · · Error '='
                    · · · ??'=='
                    · · · BinaryExpr
                    · · · · NumberLiteral '1'
                    · · · · '+'
                    · · · · BinaryExpr
                    · · · · · NumberLiteral '2'
                    · · · · · '*'
                    · · · · · NumberLiteral '3'
                    · · Arm
                    · · · '=>'
                    · · · TrueLiteral 'true'
                    · ';'
                    """);
            
            
            [Fact]
            public void EqualAsArm_1()
                => InlineSnapshot.Validate(Tree("fn A() = 1;"), """
                    ERROR UnexpectedToken@[7, 8): Expected '=>', got '='.


                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · Arm
                    · · Error '='
                    · · ??'=>'
                    · · NumberLiteral '1'
                    · ';'
                    """);
            
            [Fact]
            public void EqualAsArm_2()
                => InlineSnapshot.Validate(Tree("loop = 1;"), """
                    ERROR UnexpectedToken@[5, 6): Expected '=>', got '='.


                    ExprStmt
                    · LoopExpr
                    · · 'loop'
                    · · Arm
                    · · · Error '='
                    · · · ??'=>'
                    · · · NumberLiteral '1'
                    · ';'
                    """);
            
            [Fact]
            public void EqualAsArm_3()
                => InlineSnapshot.Validate(Tree("if true => 1 else = 2;"), """
                    ERROR UnexpectedToken@[18, 19): Expected '=>', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · TrueLiteral 'true'
                    · · Arm
                    · · · '=>'
                    · · · NumberLiteral '1'
                    · · 'else'
                    · · Arm
                    · · · Error '='
                    · · · ??'=>'
                    · · · NumberLiteral '2'
                    · ';'
                    """);
            
            [Fact]
            public void EqualAsArm_4()
                => InlineSnapshot.Validate(Tree("{ = 1 }"), """
                    ERROR UnexpectedToken@[2, 3): Expected '=>', got '='.


                    ExprStmt
                    · BlockExpr
                    · · '{'
                    · · Arm
                    · · · Error '='
                    · · · ??'=>'
                    · · · NumberLiteral '1'
                    · · '}'
                    """);
        }
    }
}