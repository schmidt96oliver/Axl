using Axl;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

switch (args[0])
{
    case "lsp":
        await Axl.Lsp.Lsp.RunAsync();
        break;
    
    case "play":
        UiPlayground.Run();
        break;

    case "run":
    {
        var sourceText = SourceText.LoadFile(UiPlayground.TestFilePath);
        var compilation = Compilation.From(Parser.Parse(sourceText));
        BoundTreeInterpreter.Run(compilation.BoundFile, Console.Out);
        break;
    }
    
    default:
        Console.WriteLine("Invalid command line. Try 'lsp' or 'play'.");
        break;
}