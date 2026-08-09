using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Spectre.Console;

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
                AnsiConsole.WriteException(e);
                continue;
            }
            
            if (source.File.Text == prevText)
                continue;
            
            prevText = source.File.Text;
            
            AnsiConsole.Clear();
            RenderSpectreFile(source);
        }
    }

    private static void RenderSpectreFile(SourceFileView source)
    {
        var syntaxTree = Parser.Parse(source);
        
        // Diagnostics:
        foreach (var diagnostic in syntaxTree.Diagnostics)
        {
            var markup = diagnostic.DefaultSeverity switch
            {
                DiagnosticSeverity.Error => "[bold Red]",
                DiagnosticSeverity.Warning => "[bold Gold1]",
                _ => "[bold SteelBlue1]"
            };

            AnsiConsole.Markup($"{markup}{diagnostic.Id}[/] ");
            var locationStrs = string.Join(" ", diagnostic.Locations.Select(GetLocationText));
            AnsiConsole.Write(locationStrs);
            AnsiConsole.WriteLine($":  {diagnostic.Message}");
            foreach (var relatedLabel in diagnostic.Related)
            {
                AnsiConsole.MarkupInterpolated($"  [underline]related[/]@{GetLocationText(relatedLabel.Location)}: {relatedLabel.Label}");
            }
        }

        AnsiConsole.WriteLine();
        
        AnsiConsole.Write(BuildTree(syntaxTree, source));
    }

    private static string GetLocationText(SourceLocation location)
    {
        var startLinePos = location.File.GetLinePositionOrEof(location.Span.First);
        var endLinePos = location.File.GetLinePositionOrEof(location.Span.End);
        
        if (startLinePos.Line == endLinePos.Line)
            return $"l.{startLinePos.Line} @ {startLinePos.Column}-{endLinePos.Column}";

        return $"l.{startLinePos.Line}@{startLinePos.Column} - l.{endLinePos.Line}@{endLinePos.Column}";
    }

    private static Tree BuildTree(SyntaxTree syntaxTree, SourceFileView source)
    {
        var tree = new Tree("Root");
        
        AddChildren(tree, syntaxTree.Root, syntaxTree, source, onErrorNode: false);
        return tree;
    }
    
    private static void AddChildren(IHasTreeNodes treeNode, SyntaxNode node, SyntaxTree syntaxTree, SourceFileView source, bool onErrorNode)
    {
        foreach (var child in node.Children)
        {
            if (child is Token { Kind.IsTrivia: false } token)
            {
                treeNode.AddNode(GetTokenText(token, onErrorNode));
            }
            else if (child is SyntaxNode childNode)
            {
                var color = onErrorNode ? "[red]" :
                    childNode.Kind is SyntaxKind.Error ? "[bold red]" : "[bold green]";
                
                var nonTrivia = childNode.Children
                    .Where(t => t is Token {Kind.IsTrivia: false} or SyntaxNode)
                    .ToList();
                if (nonTrivia.All(el => el is Token))
                {
                    var text = $"{color}{childNode.Kind}[/] ";
                    
                    text += string.Join(" ", nonTrivia
                        .OfType<Token>()
                        .Select(t => GetTokenText(t, onErrorNode || childNode.Kind is SyntaxKind.Error)));
                    
                    treeNode.AddNode(text);
                    continue;
                }
                
                var childTree = treeNode.AddNode($"{color}{childNode.Kind}[/]");
                AddChildren(childTree, childNode, syntaxTree, source, onErrorNode: childNode.Kind is SyntaxKind.Error);
            }
        }

        string GetTokenText(Token token, bool onErrorNode)
        {
            var hasError = token.Span.Length != 0 && syntaxTree.Diagnostics.Any(diag =>
                diag.DefaultSeverity is DiagnosticSeverity.Error &&
                diag.Locations.Any(location =>
                    location.Span.Contains(token.Span.First) || location.Span.Contains(token.Span.End - 1)));
            var hasEndError = syntaxTree.Diagnostics.Any(diag =>
                diag.DefaultSeverity is DiagnosticSeverity.Error &&
                diag.Locations.Any(location => location.Span.First == token.Span.End));
            var text = source.GetText(token.Span);
            return string.Concat(hasError
                    ? $"[underline red]'{text}'[/]"
                    : onErrorNode
                        ? $"[red]'{text}'[/]"
                        : $"[khaki1]'{text}'[/]",
                hasEndError ? $"[underline red]?[/]" : "");
        }
    }
}