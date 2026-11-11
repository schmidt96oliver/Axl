using AxlCompiler;
using AxlCompiler.Diagnostics;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Testing;
using AxlCompiler.Text;

namespace Axl.Tests.Compiler;

public partial class SemanticModelTests
{
    public sealed class ReferencedSymbol
    {
        [Theory]
        [Corpus]
        public void Corpus_AllNameRef_NonNullSymbol(string path)
        {
            var compilation = Compilation.From(TestFile.From(SourceText.LoadFile(path))).WithBaseLibrary();
            var nameRefs = compilation.SyntaxTrees[0].FileSyntax.Traverse()
                .OfType<NameRefSyntax>()
                .ToList();
            if (nameRefs.Any(nameRef => nameRef.Parent is ArgSyntax))
                Assert.Skip("Struct arguments not handled yet.");   //TODO: Handle struct arguments
            var nullSymbols = nameRefs
                .Where(nameRef => compilation.SemanticModel.InternalReferencedSymbolFor(nameRef) is null)
                .ToList();
            if (nullSymbols.Count > 0)
            {
                Assert.Fail("Not all name refs have a symbol reference:\n" + string.Join("\n", 
                    nullSymbols.Select(nameRef => $"\'{nameRef.Text}\' in \'{nameRef.Parent?.Location.Text.ToString().Replace("\n", " ") ?? ""}\'")));
            }
        }
    }
}