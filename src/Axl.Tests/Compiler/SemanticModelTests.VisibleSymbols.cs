using AxlCompiler;
using AxlCompiler.Symbols;
using AxlCompiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler;

public sealed partial class SemanticModelTests
{
    public sealed class VisibleSymbols
    {
        /// <summary>
        /// Prints visible symbols at `|` inside <paramref name="input"/>.
        /// </summary>
        private string VisibleSymbolsAt(string input, bool showBase = false)
        {
            input = input.RemoveCaret(out var caret);
            var compilation = Compilation.From(SourceText.From(input)).WithBaseLibrary();
            var visibleSymbols = compilation.SemanticModel
                .VisibleSymbolsAtUnmapped(compilation.SyntaxTrees[0], caret)
                .Where(s => showBase || !compilation.ReferencedCompilations.Contains(s.Compilation));
            return string.Join('\n', visibleSymbols.Select(s => $"{s.Name} ({s.FullDisplayText})"));
        }

        [Fact]
        public void Shadowing_1()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 10;
                                                        {
                                                            let a = "";
                                                            |
                                                        }
                                                        """), "a (let a: Base.String)");

        [Fact]
        public void Shadowing_2()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        struct Test;
                                                        let Test = 10;
                                                        |
                                                        """), "Test (let Test: Base.I32)");

        [Fact]
        public void Shadowing_3()
            // Here, P is shadowed, but lookup is specific to types only.
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        struct P { pub X: I32; }
                                                        let P = 2;
                                                        let a: |
                                                        """), "P (struct P)");

        [Fact]
        public void Shadowing_4()
            // Here, P is shadowed normally.
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        struct P { pub X: I32; }
                                                        let P: I32 = 2;
                                                        |
                                                        let P: String = "";
                                                        """), "P (let P: Base.I32)");


        [Fact]
        public void Script_1()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 10;
                                                        |
                                                        let b = 11;
                                                        """), "a (let a: Base.I32)");

        [Fact]
        public void Script_2()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 10;
                                                        let b = |
                                                        let c = 11;
                                                        """), "a (let a: Base.I32)");

        [Fact]
        public void Script_3()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 10;
                                                        if (|
                                                        """), "a (let a: Base.I32)");

        [Fact]
        public void Script_4()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 10;
                                                        { |
                                                        """), "a (let a: Base.I32)");

        [Fact]
        public void Member_1()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 10;
                                                        a.|
                                                        """, showBase: true), "ToString (fun Base.I32.ToString())");

        [Fact]
        public void Member_2()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        Base.|
                                                        """, showBase: true), """
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
        public void Comment()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 10;
                                                        // |
                                                        """), "");

        [Fact]
        public void String()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = "|";
                                                        """), "");

        [Fact]
        public void StringInterpolation()
            => InlineSnapshot.Validate(VisibleSymbolsAt("""
                                                        let a = 1;
                                                        let b = "{|
                                                        """), "a (let a: Base.I32)");
    }
}