using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class ModuleDecl
        {
            [Fact]
            public void MissingBodyAndSemicolon_Global()
                => InlineSnapshot.Validate(Tree("module A"), """
                    ERROR MissingToken@[8, 8): Expected '{'.


                    ModuleDecl
                    · 'module'
                    · Path
                    · · IdName 'A'
                    · ??'{'
                    · ??'}'
                    """);
            
            [Fact]
            public void MissingBodyAndSemicolon_InModule()                          
                => InlineSnapshot.Validate(Tree("module A { module B }"), """
                    ERROR MissingToken@[19, 19): Expected '{'.


                    ModuleDecl
                    · 'module'
                    · Path
                    · · IdName 'A'
                    · '{'
                    · ModuleDecl
                    · · 'module'
                    · · Path
                    · · · IdName 'B'
                    · · ??'{'
                    · · ??'}'
                    · '}'
                    """);

            [Fact]
            public void ErrorWithBracesInsideBody()
                => InlineSnapshot.Validate(Tree("""
                                                module A
                                                {
                                                  {}
                                                  
                                                  fn Survives() => 1;
                                                } 
                                                """
                ), """
                    ERROR UnexpectedToken@[15, 16): Expected a member ('fn' or 'module'), got '{'.


                    ModuleDecl
                    · 'module'
                    · Path
                    · · IdName 'A'
                    · '{'
                    · Garbage '{' '}'
                    · FnDecl
                    · · 'fn'
                    · · IdName 'Survives'
                    · · ParamList '(' ')'
                    · · Arm
                    · · · '=>'
                    · · · NumberLiteral '1'
                    · · ';'
                    · '}'
                    """);

            [Fact]
            public void FileScoped_InModule()
                // B must be part of A, not A.Global
                => InlineSnapshot.Validate(Tree("""
                                                module A
                                                { 
                                                    module Global; 
                                                    module B { }
                                                }
                                                """), """
                    ModuleDecl
                    · 'module'
                    · Path
                    · · IdName 'A'
                    · '{'
                    · FileScopedModuleDecl
                    · · 'module'
                    · · Path
                    · · · IdName 'Global'
                    · · ';'
                    · ModuleDecl
                    · · 'module'
                    · · Path
                    · · · IdName 'B'
                    · · '{'
                    · · '}'
                    · '}'
                    """);
            [Fact]
            public void FileScoped_InFnBody()
                // 1; must be part of fn A; not garbage
                => InlineSnapshot.Validate(Tree("""
                                                fn A()
                                                { module Global; 1; }
                                                """), """
                    FnDecl
                    · 'fn'
                    · IdName 'A'
                    · ParamList '(' ')'
                    · BlockExpr
                    · · '{'
                    · · FileScopedModuleDecl
                    · · · 'module'
                    · · · Path
                    · · · · IdName 'Global'
                    · · · ';'
                    · · ExprStmt
                    · · · NumberLiteral '1'
                    · · · ';'
                    · · '}'
                    """);
            [Fact]
            public void FileScoped_InBlock()
                => InlineSnapshot.Validate(Tree("{ module Global; => 1 }"), """
                    ExprStmt
                    · BlockExpr
                    · · '{'
                    · · FileScopedModuleDecl
                    · · · 'module'
                    · · · Path
                    · · · · IdName 'Global'
                    · · · ';'
                    · · Arm
                    · · · '=>'
                    · · · NumberLiteral '1'
                    · · '}'
                    """);
            
            [Fact]
            public void Stmt_AfterFileScoped()
                // Must be garbage inside file-scoped
                => InlineSnapshot.Validate(Tree("""
                                                module Global;
                                                var a = 1;
                                                """), """
                    ERROR UnexpectedToken@[16, 19): Expected a member ('fn' or 'module'), got 'var'.


                    FileScopedModuleDecl
                    · 'module'
                    · Path
                    · · IdName 'Global'
                    · ';'
                    · Garbage 'var' 'a' '=' '1' ';'
                    """);
        }
    }
}