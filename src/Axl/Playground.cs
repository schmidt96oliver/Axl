using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl;

public static class Playground
{
    private static string TestFilePath = Path.Combine("..", "..", "..", "..", "src", "Axl", "test.axl");

    public static void Run()
    {
        string? prevText = null;
        while (true)
        {
            Thread.Sleep(250);

            SourceFileView source;
            try
            {
                source = SourceFileView.FromFile(TestFilePath);
                
            }
            catch (IOException e)
            {
                Console.Clear();
                Console.WriteLine(e);
                continue;
            }
            
            if (source.File.Text == prevText)
                continue;
            
            prevText = source.File.Text;
            
            Console.Clear();
            ExecuteTestFile(source);
        }
    }

    private static void ExecuteTestFile(SourceFileView source)
    {
        var tree = Parser.Parse(source);
        
        // Print diagnostics
        foreach (var diag in tree.Diagnostics)
        {
            Console.WriteLine(
                $"{diag.DefaultSeverity.ToString().ToUpper()} {diag.Id}@{diag.Location.Span}: {diag.Message}");
            foreach (var related in diag.Related)
                Console.WriteLine($"   related@{related.Location.Span}: {related.Label}");
        }
        
        Console.WriteLine("//=== tree");
        
        Print(tree.Root, "");
        return;
        
        void Print(SyntaxElement element, string prefix)
        {
            switch (element)
            {
                case Token { Kind.IsTrivia: false } token:
                {
                    var text = source.GetText(token.Span).ToString().Replace("\n", "\\n").Replace("\r", "\\r");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(prefix);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"{token.Kind}: ");
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"\"{text}\"");
                    break;
                }
                
                case SyntaxNode node:
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(prefix);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write($"{node.Kind}");

                    var nonTriviaTokens = node.Children
                        .Where(t => t is Token {Kind.IsTrivia: false} or SyntaxNode)
                        .ToList();
                    if (nonTriviaTokens is [Token token])
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        var text = source.GetText(token.Span).ToString().Replace("\n", "\\n").Replace("\r", "\\r");
                        Console.WriteLine($": \"{text}\"");
                    }
                    else if (nonTriviaTokens is [])
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(node.Children.Length == 0
                            ? $" [empty]@{node.Span}"
                            : $" [only trivia]@{node.Span}");
                    }
                    else
                    {
                        Console.WriteLine();
                        foreach (var child in node.Children)
                            Print(child, prefix + "| ");
                    }
                    break;
                }
            }
        }
    }
}