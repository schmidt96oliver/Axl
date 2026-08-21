using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        public sealed class MemberDecl
        {
            [Fact]
            public void StrayModifiers_1()
                => InlineSnapshot.Validate(Tree("public"), """
                    ERROR MissingToken@[6, 6): Expected a member ('fn' or 'module').


                    Garbage 'public'
                    """);
            
            [Fact]
            public void StrayModifiers_2()
                => InlineSnapshot.Validate(Tree("public private public"), """
                    ERROR MissingToken@[21, 21): Expected a member ('fn' or 'module').


                    Garbage 'public' 'private' 'public'
                    """);
        }
    }
}