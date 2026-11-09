using AxlCompiler;
using AxlCompiler.Text;
using AxlLsp.Handlers;
using Meziantou.Framework.InlineSnapshotTesting;


namespace Axl.Tests.Lsp;

public sealed class CompletionTests
{
    private static string Items(string input, bool withBase = false)
    {
        input = input.RemoveCaret(out var caret);
        var sourceText = SourceText.From(input);
        var compilation = Compilation.From(sourceText).WithBaseLibrary();

        var itemTexts = CompletionHandler
            .GetCompletionItems(SourceLocation.EmptyAt(sourceText, caret), compilation, excludeReferencedCompilations: !withBase)
            .GroupBy(item => item.Kind)
            .Select(grp => $"{grp.Key}: { string.Join(", ", grp.Select(item => item.Label)) }")
            .Order();
        return string.Join("\n", itemTexts);
    }

    [Fact]
    public void Empty()
        => InlineSnapshot.Validate(Items("|"));

    [Fact]
    public void Comment1()
        => InlineSnapshot.Validate(Items("// p|"), "Keyword: fun, var, let, namespace, return, if, while, break, continue, using, struct, pub, static, self, mut, true, false");
}