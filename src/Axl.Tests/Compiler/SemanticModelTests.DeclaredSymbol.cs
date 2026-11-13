using AxlCompiler;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Testing;
using AxlCompiler.Text;

namespace Axl.Tests.Compiler;

public partial class SemanticModelTests
{
    public sealed class DeclaredSymbol
    {
        [Theory]
        [Corpus]
        public void Corpus_AllIdTokens_NonNullSymbol(string path)
        {
            var compilation = Compilation.From(TestFile.From(SourceText.LoadFile(path))).WithBaseLibrary();
            var tokens = compilation.SyntaxTrees
                .SelectMany(tree => tree.FileSyntax.Traverse().OfType<IdentifierToken>())
                .Where(token => token.Parent is not NameRefSyntax)
                .ToList();

            var wrongSymbols = tokens
                .Select(idToken => (Token: idToken, Fail: CheckSymbolAgainstIdToken(compilation.SemanticModel.TryDeclaredSymbolFor(idToken), idToken)))
                .Where(pair => pair.Fail is not null)
                .Select(pair =>
                    $"\'{pair.Token.Text}\' in \'{pair.Token.Parent?.Location.Text.ToString().Replace("\n", " ") ?? ""}\':\n   {pair.Fail}")
                .ToList();
            if (wrongSymbols.Count > 0)
            {
                Assert.Fail($"$Invalid declared symbols:\n{string.Join("\n", wrongSymbols)}");
            }
        }

        private string? CheckSymbolAgainstIdToken(Symbol? symbol, Token idToken)
        {
            if (symbol is null)
                return "GAP! No declared symbol!";

            if (!symbol.Name.SequenceEqual(idToken.Text))
                return $"Unexpected symbol name \'{symbol.Name}\'";

            if (!symbol.NameSyntax.Contains(idToken))
                return "Symbol does not contain the given name syntax.";

            return null;
        }
    }
}