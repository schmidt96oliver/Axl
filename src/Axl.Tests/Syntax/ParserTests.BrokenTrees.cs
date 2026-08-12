using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public partial class BrokenTrees
    {
        [Fact]
        public void EmptyNative()
            => InlineSnapshot.Validate(Tree("native() fn A();"), """
                ERROR MissingToken@[7, 7): Expected a string.


                FnDecl
                · NativeClause 'native' '(' ')'
                · 'fn'
                · IdName 'A'
                · ParamList '(' ')'
                · ';'
                """);
    }
}