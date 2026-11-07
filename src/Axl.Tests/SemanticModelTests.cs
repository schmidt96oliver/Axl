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
    private string VisibleSymbolsAt(string input)
    {
        var caret = FindCaretIndex(input);
        input = input.Remove(caret, 1);

        var compilation = Compilation.From(SourceText.From(input)).WithBaseLibrary();
        var symbols = compilation.SemanticModel.VisibleSymbolsAt(compilation.SyntaxTrees[0], caret);
        return string.Join('\n', symbols.Select(s => $"{s.Name} ({s.ShortDisplayText})"));
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
    public void VisibleSymbols_Script_1()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    |
                                                    """), """
            a (let a)
            Base (namespace 'Base')
            Bool (struct 'Base.Bool')
            F32 (struct 'Base.F32')
            F64 (struct 'Base.F64')
            I32 (struct 'Base.I32')
            I64 (struct 'Base.I64')
            Never (struct 'Base.Never')
            Panic (fun 'Base.Panic')
            Panic (fun 'Base.Panic')
            Assert (fun 'Base.Assert')
            Assert (fun 'Base.Assert')
            TODO (fun 'Base.TODO')
            Unreachable (fun 'Base.Unreachable')
            Unreachable (fun 'Base.Unreachable')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            String (struct 'Base.String')
            Unit (struct 'Base.Unit')
            """);
    [Fact]
    public void VisibleSymbols_Script_2()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    let b = |
                                                    """), """
            a (let a)
            Base (namespace 'Base')
            Bool (struct 'Base.Bool')
            F32 (struct 'Base.F32')
            F64 (struct 'Base.F64')
            I32 (struct 'Base.I32')
            I64 (struct 'Base.I64')
            Never (struct 'Base.Never')
            Panic (fun 'Base.Panic')
            Panic (fun 'Base.Panic')
            Assert (fun 'Base.Assert')
            Assert (fun 'Base.Assert')
            TODO (fun 'Base.TODO')
            Unreachable (fun 'Base.Unreachable')
            Unreachable (fun 'Base.Unreachable')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            String (struct 'Base.String')
            Unit (struct 'Base.Unit')
            """);
    [Fact]
    public void VisibleSymbols_Script_3()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    if (|
                                                    """), """
            a (let a)
            Base (namespace 'Base')
            Bool (struct 'Base.Bool')
            F32 (struct 'Base.F32')
            F64 (struct 'Base.F64')
            I32 (struct 'Base.I32')
            I64 (struct 'Base.I64')
            Never (struct 'Base.Never')
            Panic (fun 'Base.Panic')
            Panic (fun 'Base.Panic')
            Assert (fun 'Base.Assert')
            Assert (fun 'Base.Assert')
            TODO (fun 'Base.TODO')
            Unreachable (fun 'Base.Unreachable')
            Unreachable (fun 'Base.Unreachable')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            String (struct 'Base.String')
            Unit (struct 'Base.Unit')
            """);
    [Fact]
    public void VisibleSymbols_Script_4()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    { |
                                                    """), """
            a (let a)
            Base (namespace 'Base')
            Bool (struct 'Base.Bool')
            F32 (struct 'Base.F32')
            F64 (struct 'Base.F64')
            I32 (struct 'Base.I32')
            I64 (struct 'Base.I64')
            Never (struct 'Base.Never')
            Panic (fun 'Base.Panic')
            Panic (fun 'Base.Panic')
            Assert (fun 'Base.Assert')
            Assert (fun 'Base.Assert')
            TODO (fun 'Base.TODO')
            Unreachable (fun 'Base.Unreachable')
            Unreachable (fun 'Base.Unreachable')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            String (struct 'Base.String')
            Unit (struct 'Base.Unit')
            """);
    
    [Fact]
    public void VisibleSymbols_Member_1()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    let a = 10;
                                                    a.|
                                                    """), "ToString (fun 'Base.I32.ToString')");
    [Fact]
    public void VisibleSymbols_Member_2()
        => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                    Base.|
                                                    """), """
            Bool (struct 'Base.Bool')
            F32 (struct 'Base.F32')
            F64 (struct 'Base.F64')
            I32 (struct 'Base.I32')
            I64 (struct 'Base.I64')
            Never (struct 'Base.Never')
            Panic (fun 'Base.Panic')
            Panic (fun 'Base.Panic')
            Assert (fun 'Base.Assert')
            Assert (fun 'Base.Assert')
            TODO (fun 'Base.TODO')
            Unreachable (fun 'Base.Unreachable')
            Unreachable (fun 'Base.Unreachable')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            String (struct 'Base.String')
            Unit (struct 'Base.Unit')
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
                                                    """), """
            a (let a)
            Base (namespace 'Base')
            Bool (struct 'Base.Bool')
            F32 (struct 'Base.F32')
            F64 (struct 'Base.F64')
            I32 (struct 'Base.I32')
            I64 (struct 'Base.I64')
            Never (struct 'Base.Never')
            Panic (fun 'Base.Panic')
            Panic (fun 'Base.Panic')
            Assert (fun 'Base.Assert')
            Assert (fun 'Base.Assert')
            TODO (fun 'Base.TODO')
            Unreachable (fun 'Base.Unreachable')
            Unreachable (fun 'Base.Unreachable')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            Print (fun 'Base.Print')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            PrintLine (fun 'Base.PrintLine')
            String (struct 'Base.String')
            Unit (struct 'Base.Unit')
            """);
}