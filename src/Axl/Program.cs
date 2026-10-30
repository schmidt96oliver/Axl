using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Mir;
using Axl.Compiler.Symbols;
using Axl.Compiler.Testing;
using Axl.Compiler.Text;

var testDirectory = Path.Combine("..", "..", "..", "..", "src", "Axl");
var testFilePath = Path.Combine(testDirectory, "test.taxl");

switch (args[0])
{
    case "lsp":
        await Axl.Lsp.Lsp.RunAsync();
        break;

    case "run":
    {
        var sourceText = SourceText.LoadFile(testFilePath);
        var testFile = Path.GetExtension(testFilePath) is ".taxl"
            ? TestFile.From(sourceText)
            : null;
        var compilation = Compilation
            .From(testFile is not null
                ? testFile.Fragments.Select(f => f.SourceText)
                : [sourceText])
            .AddBaseLibrary();

        if (compilation.Scripts.Length != 1)
        {
            Console.WriteLine($"Compilation has {compilation.SyntaxTrees.Length} scripts, but can only run 1.");
            return;
        }

        var allDiagnostics =
            compilation.Diagnostics.Concat(compilation.ReferencedCompilations.SelectMany(c => c.Diagnostics))
                .ToImmutableArray();

        foreach (var diagnostic in allDiagnostics)
        {
            Console.WriteLine(testFile is not null
                ? diagnostic.GetDisplayString(testDirectory,
                    l => testFile.TryMapToTestFile(l) ?? l)
                : diagnostic.GetDisplayString(testDirectory));
        }

        foreach (var fun in compilation.AllFuns)
        {
            Console.WriteLine(compilation.GetLoweredFunBody(fun).MirFunction.Print());
            Console.WriteLine();
        }

        if (!allDiagnostics.HasError)
            Miri.Run(compilation.Lower(compilation.Scripts[0]), Console.Out);

        break;
    }

    default:
        Console.WriteLine("Invalid command line. Try 'lsp' or 'play'.");
        break;
}

return;