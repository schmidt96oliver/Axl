
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Taxl;

var prevText = "";
while (true)
{
    Thread.Sleep(250);
    
    var view = SourceView.FromFile("../../../../src/Axl/lexText.taxl");
    if (view.File.Text == prevText)
        continue;
    
    prevText = view.File.Text;
    
    var bag = new DiagnosticBag();
    var tokens = TaxlLexer.Lex(view, bag);

    Console.Clear();
    foreach (var diagnostic in bag.Diagnostics)
    {
        Console.WriteLine(
            $"[{diagnostic.DefaultSeverity.ToString().ToUpper()}] {diagnostic.Id}@{diagnostic.Location.Span}: {diagnostic.Message}");
    }

    Console.WriteLine("-- Tokens --");

    tokens.RemoveAll(token => token.Kind is TaxlTokenKind.Whitespace or TaxlTokenKind.Newline or TaxlTokenKind.Comment);
    foreach (var token in tokens)
    {
        Console.WriteLine(token.Kind is not TaxlTokenKind.Newline
            ? $"- {token.Kind}: \"{token.Text}\""
            : $"- {token.Kind}");
    }
}

await Axl.Lsp.Lsp.RunAsync();