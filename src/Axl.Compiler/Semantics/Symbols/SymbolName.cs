using System.IO.Compression;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// The name of a <see cref="Symbol"/>. It is never a path and does not
/// contain dots.
/// </summary>
public readonly record struct SymbolName
{
    public string Text { get; }

    public bool IsEmpty => Text.Length == 0;
    

    private SymbolName(string text)
    {
        Text = text;
    }

    public static SymbolName From(ReadOnlySpan<char> text)
    {
        Guard.MustBe(!text.Contains('.'));
        return new SymbolName(text.Trim().ToString());
    }

    public static SymbolName From(IdentifierToken token)
        => From(token.Identifier);
    
    public static SymbolName From(IdNameSyntax idNameSyntax)
        => From(idNameSyntax.Token.Identifier);


    public static implicit operator string(SymbolName symbolName)
        => symbolName.Text;

    public override string ToString()
        => Text;
}