using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public readonly record struct SymbolName
{
    public string Text { get; private init; }

    private SymbolName(string text)
    {
        Text = text;
    }

    public static SymbolName From(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException();
        return new SymbolName(text);
    }


    public static SymbolName From(IdentifierToken token)
    {
        Guard.MustBe(!token.IsMissing);
        return From(token.Identifier);
    }

    public static SymbolName From(IdNameSyntax idNameSyntax)
        => From(idNameSyntax.Token);


    public static implicit operator string(SymbolName symbolName)
        => symbolName.Text;
}