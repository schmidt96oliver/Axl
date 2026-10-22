using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Testing;
using Axl.Compiler.Text;

var testFilePath = Path.Combine("..", "..", "..", "..", "src", "Axl", "test.taxl");

switch (args[0])
{
    case "lsp":
        await Axl.Lsp.Lsp.RunAsync();
        break;
    
    case "run":
    {
        var sourceText = SourceText.LoadFile(testFilePath);
        var compilation = Path.GetExtension(testFilePath) is ".taxl"
            ? TestFile.From(sourceText).Compilation
            : Compilation.From(sourceText);

        if (compilation.Scripts.Length != 1)
        {
            Console.WriteLine($"Compilation has {compilation.SyntaxTrees.Length} scripts, but can only run 1.");
            return;
        }

        foreach (var diagnostic in compilation.Diagnostics)
            Console.WriteLine(
                $"[{diagnostic.DefaultSeverity.ToString().ToUpperInvariant()}] {diagnostic.Id}@{diagnostic.Locations[0].Range}: {diagnostic.Message}");
        
        if (!compilation.Diagnostics.HasError)
            BoundTreeInterpreter.Run(compilation.Scripts[0].Fun, Console.Out);
        break;
    }
    
    default:
        Console.WriteLine("Invalid command line. Try 'lsp' or 'play'.");
        break;
}