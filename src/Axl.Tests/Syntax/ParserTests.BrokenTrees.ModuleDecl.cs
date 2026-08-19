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
        }
    }
}