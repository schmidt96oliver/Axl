using Axl.Compiler;
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
        var compilation = Compilation.From(sourceText);

        if (compilation.SyntaxTrees.Length != 1)
        {
            Console.WriteLine($"Compilation has {compilation.SyntaxTrees.Length} files, but can only run 1.");
            return;
        }
        
        BoundTreeInterpreter.Run(compilation.Scripts[0].Fun, Console.Out);
        break;
    }
    
    default:
        Console.WriteLine("Invalid command line. Try 'lsp' or 'play'.");
        break;
}