using Axl;

switch (args[0])
{
    case "lsp":
        await Axl.Lsp.Lsp.RunAsync();
        break;
    
    case "play":
        UiPlayground.Run();
        break;
    
    default:
        Console.WriteLine("Invalid command line. Try 'lsp' or 'play'.");
        break;
}