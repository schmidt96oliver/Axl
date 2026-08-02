
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

var path = Path.Combine("..", "..", "..", "..", "src", "Axl", "test.axl");

var prevText = "";

while (true)
{
    Thread.Sleep(250);

    var bag = new DiagnosticBag();
    var source = SourceFileView.FromFile(path);
    if (source.File.Text == prevText)
        continue;


    prevText = source.File.Text;
    Console.Clear();



    var lex = Lexer.Lex(source, bag);

    // Print diagnostics
    if (bag.Diagnostics.Count > 0)
    {
        foreach (var diag in bag.Diagnostics)
            Console.WriteLine(
                $"{diag.DefaultSeverity.ToString().ToUpper()} {diag.Id}@{diag.Location.Span}: {diag.Message}");
    }

    // Print tokens
    foreach (var token in lex)
    {
        var text = source.GetText(token.Span).Replace("\n", "\\n");
        Console.WriteLine($"- {token.Kind}: \"{text}\"");
    }


}

//await Axl.Lsp.Lsp.RunAsync();