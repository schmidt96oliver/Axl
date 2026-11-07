using Axl.Compiler;
using Axl.Compiler.Symbols;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests;

public sealed class SemanticModelTests
{
    /// <summary>
    /// Prints visible symbols at `|` inside <paramref name="input"/>.
    /// </summary>
    private string VisibleSymbolsAt(string input, bool noBase = false)
    {
        var caret = FindCaretIndex(input);
        input = input.Remove(caret, 1);

        var compilation = Compilation.From(SourceText.From(input)).WithBaseLibrary();
        var symbols = compilation.SemanticModel.VisibleSymbolsAt(compilation.SyntaxTrees[0], caret);
        return string.Join('\n', symbols.Where(s => !noBase || s.Owner?.Name is not "Base")
            .Select(s => $"{s.Name} ({s.FullDisplayText})"));
    }

    private int FindCaretIndex(string input)
    {
        // `|` will denote a caret, only if it is not part of a group
        // of vertical bars like `||`. This will denote other things.
        
        var caret = input.IndexOf('|');
        while (caret != -1)
        {
            var prev = caret > 0 ? input[caret - 1] : '\0';
            var next = caret < input.Length - 1 ? input[caret + 1] : '\0';
            if (prev != '|' && next != '|')
                return caret;
            
            if (caret < input.Length - 1)
                caret = input.IndexOf('|', caret + 1);
            else
                break;
        }

        throw new InvalidOperationException("Input has no caret.");
    }
    
    
    [Fact]
    public void VisibleSymbols_Shadowing_1()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    {
                                                        let a = "";
                                                        |
                                                    }
                                                    """, noBase: true));
    [Fact]
    public void VisibleSymbols_Shadowing_2()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    struct Test;
                                                    let Test = 10;
                                                    |
                                                    """, noBase: true));
    
    
    [Fact]
    public void VisibleSymbols_Script_1()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    |
                                                    """, noBase: true), """
            a (let a: Base.I32)
            Base (namespace Base)
            """);
    [Fact]
    public void VisibleSymbols_Script_2()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    let b = |
                                                    """, noBase: true), """
            a (let a: Base.I32)
            Base (namespace Base)
            """);
    [Fact]
    public void VisibleSymbols_Script_3()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    if (|
                                                    """, noBase: true), """
            a (let a: Base.I32)
            Base (namespace Base)
            """);
    [Fact]
    public void VisibleSymbols_Script_4()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    { |
                                                    """, noBase: true), """
            a (let a: Base.I32)
            Base (namespace Base)
            """);
    
    [Fact]
    public void VisibleSymbols_Member_1()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    a.|
                                                    """), "ToString (fun Base.I32.ToString())");
    [Fact]
    public void VisibleSymbols_Member_2()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    Base.|
                                                    """), """
            Bool (struct Base.Bool)
            F32 (struct Base.F32)
            F64 (struct Base.F64)
            I32 (struct Base.I32)
            I64 (struct Base.I64)
            Never (struct Base.Never)
            Panic (fun Base.Panic(message: Base.String))
            Panic (fun Base.Panic())
            Assert (fun Base.Assert(condition: Base.Bool, message: Base.String))
            Assert (fun Base.Assert(condition: Base.Bool))
            TODO (fun Base.TODO())
            Unreachable (fun Base.Unreachable())
            Unreachable (fun Base.Unreachable(message: Base.String))
            Print (fun Base.Print(string: Base.String))
            Print (fun Base.Print(int: Base.I32))
            Print (fun Base.Print(int: Base.I64))
            Print (fun Base.Print(float: Base.F32))
            Print (fun Base.Print(float: Base.F64))
            Print (fun Base.Print(bool: Base.Bool))
            PrintLine (fun Base.PrintLine())
            PrintLine (fun Base.PrintLine(string: Base.String))
            PrintLine (fun Base.PrintLine(int: Base.I32))
            PrintLine (fun Base.PrintLine(int: Base.I64))
            PrintLine (fun Base.PrintLine(float: Base.F32))
            PrintLine (fun Base.PrintLine(float: Base.F64))
            PrintLine (fun Base.PrintLine(bool: Base.Bool))
            String (struct Base.String)
            Unit (struct Base.Unit)
            """);


    [Fact]
    public void VisibleSymbols_Comment()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    // |
                                                    """), "");
    [Fact]
    public void VisibleSymbols_String()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = "|";
                                                    """), "");
    [Fact]
    public void VisibleSymbols_StringInterpolation()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 1;
                                                    let b = "{|
                                                    """, noBase: true), """
            a (let a: Base.I32)
            Base (namespace Base)
            """);
}