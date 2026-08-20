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
                => InlineSnapshot.Validate(Tree("if true = 1;"), """
                    ERROR UnexpectedToken@[8, 9): Expected '=>', got '='.


                    ExprStmt
                    · IfExpr
                    · · 'if'
                    · · TrueLiteral 'true'
                    · · Arm
                    · · · Error '='
                    · · · ??'=>'
                    · · · NumberLiteral '1'
                    · ';'
                    """);
            
            [Fact]
            public void EqualAsArm_3()
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
            public void EqualAsArm_4()
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
            public void EqualAsArm_5()
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