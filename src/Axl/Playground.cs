using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl;

public static class Playground
{
    private static string TestFilePath = Path.Combine("..", "..", "..", "..", "src", "Axl", "test.axl");

    public static void Run()
    {
        var prevText = "";
        while (true)
        {
            Thread.Sleep(250);

            var source = SourceFileView.FromFile(TestFilePath);
            if (source.File.Text == prevText)
                continue;

            prevText = source.File.Text;
            
            Console.Clear();
            ExecuteTestFile(source);
        }
    }

    private static void ExecuteTestFile(SourceFileView source)
    {
        var bag = new DiagnosticBag();
        var lex = Lexer.Lex(source, bag);

        // Print diagnostics
        foreach (var diag in bag.Drain())
            Console.WriteLine(
                $"{diag.DefaultSeverity.ToString().ToUpper()} {diag.Id}@{diag.Location.Span}: {diag.Message}");


        // Print tokens
        foreach (var token in lex /*.Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.Comment))*/)
        {
            var text = source.GetText(token.Span).ToString().Replace("\n", "\\n");
            Console.WriteLine($"- {token.Kind}: \"{text}\"");
        }
    }
}