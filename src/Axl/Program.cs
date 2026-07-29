
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
    foreach (var token in tokens.Where(token =>
                 token.Kind is not (TaxlTokenKind.Whitespace or TaxlTokenKind.Newline or TaxlTokenKind.Comment)))
    {
        if (token is TaxlToken.AxlTextToken textToken)
        {
            Console.Write($"- {token.Kind}: ");
            if (textToken.Text.Contains('\n'))
            {
                Console.WriteLine();
                Console.WriteLine(textToken.Text);
            }
            else 
                Console.WriteLine($"\"{textToken.Text}\"");
            
            if (textToken.InTextTokens.Length >0)
            {
                Console.WriteLine("--> In-Text Tokens:");
                foreach (var inTextToken in textToken.InTextTokens.Where(tkn =>
                             tkn.Kind is not (TaxlTokenKind.Whitespace or TaxlTokenKind.Newline or TaxlTokenKind.Comment)))
                {
                    Console.WriteLine(inTextToken.Kind is not TaxlTokenKind.Newline
                        ? $"   - {inTextToken.Kind}: \"{inTextToken.Text}\""
                        : $"   - {inTextToken.Kind}");
                }
            }
        }
        else
        {
            Console.WriteLine(token.Kind is not TaxlTokenKind.Newline
                ? $"- {token.Kind}: \"{token.Text}\""
                : $"- {token.Kind}");
        }
    }
}

await Axl.Lsp.Lsp.RunAsync();