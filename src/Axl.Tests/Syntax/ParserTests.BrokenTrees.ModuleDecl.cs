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
                    ERROR MissingToken@[8, 8): Expected ';'.


                    GlobalModuleDecl
                    · 'module'
                    · QualifiedName
                    · · IdName 'A'
                    · ??';'
                    """);
            
            [Fact]
            public void MissingBodyAndSemicolon_InModule()                          
                => InlineSnapshot.Validate(Tree("module A { module B }"), """
                    ERROR MissingToken@[19, 19): Expected '{'.


                    ModuleDecl
                    · 'module'
                    · QualifiedName
                    · · IdName 'A'
                    · '{'
                    · ModuleDecl
                    · · 'module'
                    · · QualifiedName
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
                    · QualifiedName
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
        }
    }
}