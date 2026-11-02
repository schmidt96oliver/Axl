using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Mir;
using Axl.Compiler.Mir.Analysis;
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

        var result = compilation.Compile(compilation.Scripts[0]);
        
        foreach (var diagnostic in result.Diagnostics)
        {
            Console.WriteLine(testFile is not null
                ? diagnostic.GetDisplayString(testDirectory,
                    l => testFile.TryMapToTestFile(l) ?? l)
                : diagnostic.GetDisplayString(testDirectory));
        }

        if (!result.IsSuccessful)
            return;

        Console.WriteLine("---------------");
        Console.WriteLine("--- GENERIC ---");
        Console.WriteLine("---------------");
        
        foreach (var fun in compilation.AllFuns.Select(compilation.GetLoweredFunBody))
        {
            Console.WriteLine(fun.MirFunction.Print());
            Console.WriteLine();
        }
        
        Console.WriteLine("---------------");
        Console.WriteLine("--- MONOMOR ---");
        Console.WriteLine("---------------");
        
        foreach (var fun in result.Program!.MirFunctions.Where(f => !f.DisplayName.StartsWith("Base")).OrderBy(f => f.DisplayName, StringComparer.Ordinal))
        {
            Console.WriteLine(fun.Print());
            Console.WriteLine();
        }
        
        Console.WriteLine("----------------");
        Miri.Run(result.Program!, Console.Out);

        break;
    }

    default:
        Console.WriteLine("Invalid command line. Try 'lsp' or 'play'.");
        break;
}

return;