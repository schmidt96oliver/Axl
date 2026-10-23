using Axl.Compiler;
using Axl.Compiler.Diagnostics;
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
        var compilation = testFile is not null ? testFile.Compilation : Compilation.From(sourceText);

        if (compilation.Scripts.Length != 1)
        {
            Console.WriteLine($"Compilation has {compilation.SyntaxTrees.Length} scripts, but can only run 1.");
            return;
        }

        foreach (var diagnostic in compilation.Diagnostics)
        {
            Console.WriteLine(testFile is not null
                ? diagnostic.GetDisplayString(testDirectory,
                    testFile.MapToTestFile)
                : diagnostic.GetDisplayString(testDirectory));
        }
        
        if (!compilation.Diagnostics.HasError)
            BoundTreeInterpreter.Run(compilation.Scripts[0].Fun, Console.Out);
        break;
    }
    
    default:
        Console.WriteLine("Invalid command line. Try 'lsp' or 'play'.");
        break;
}