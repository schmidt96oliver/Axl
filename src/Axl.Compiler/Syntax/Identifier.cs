using System.Diagnostics;

namespace Axl.Compiler.Syntax;

public readonly record struct Identifier
{
    /// <summary>
    /// Non-empty. Don't use it. Use comparison
    /// on <see cref="Identifier"/> directly where possible.
    /// </summary>
    public string Text { get; }
    
    private Identifier(string text)
    {
        Text = text;
    }

    public override string ToString() => Text;
    
    /// <summary>
    /// Only to be called from the lexer. Only lexer
    /// can create identifiers.
    /// </summary>
    internal static Identifier FromLexer(string text)
    {
        Debug.Assert(!string.IsNullOrEmpty(text));
        return new Identifier(text);
    }
}