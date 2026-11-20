using AxlCompiler;
using AxlCompiler.Symbols;
using AxlCompiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler;

public partial class SemanticModelTests
{
    public sealed class MinimalDisplayText
    {
        /// <summary>
        /// Each input is its own file. Carets can be placed in any of them and
        /// must touch an identifier. Prints what hover would show there.
        /// </summary>
        private string Info(params string[] inputs)
        {
            List<(int File, int Caret)> carets = [];
            for (var file = 0; file < inputs.Length; file++)
            {
                while (true)
                {
                    inputs[file] = inputs[file].RemoveCaret(out var caret);
                    if (caret < 0) break;
                    carets.Add((file, caret));
                }
            }

            var compilation = Compilation.From(inputs.Select(input => SourceText.From(input))).WithBaseLibrary();
            var model = compilation.SemanticModel;
            var texts = carets
                .Select(c => compilation.SyntaxTrees[c.File].TokenAt(c.Caret))
                .Select(token => $"'{token.Text}' " + model.SymbolFor(token) switch
                {
                    SymbolReference.Resolved resolved
                        => resolved.Symbol.MinimalDisplayText(model, resolved.ReferenceLocation),
                    SymbolReference.Unresolved unresolved => $"Unresolved ({unresolved.Reason}):\n   {
                        string.Join("\n   ", unresolved.Candidates.Select(s => s.MinimalDisplayText(model, unresolved.ReferenceLocation)))}"
                });

            return string.Join("\n", texts);
        }

        [Fact]
        public void NothingShadowed()
            => InlineSnapshot.Validate(Info("""
                                            namespace Geo;
                                            pub struct Point { pub X: I32; }
                                            """,
                                            """
                                            fun Move(p: Geo.Point, by: I32): Geo.Point = p;
                                            let p = Geo.Point(X = 1);
                                            |p;                     // let p: Geo.Point     (Geo needed, Base not)
                                            |Move(p, 2);            // fun Move(p: Geo.Point, by: I32)
                                            let s: |String = "";    // struct String
                                            """), """
                'p' let p: Geo.Point
                'Move' fun Move(p: Geo.Point, by: I32)
                'String' struct String
                """);

        [Fact]
        public void BaseTypeShadowed()
            => InlineSnapshot.Validate(Info("""
                                            struct String;
                                            fun Take(s: |String, i: I32) {     // struct String   (the user's)
                                                |s;                 // parameter s: String
                                                |i;                 // parameter i: I32     (unrelated Base type stays short)
                                            }
                                            let a = "";
                                            |a;                     // let a: Base.String   (user String shadows it)
                                            """), """
                'String' struct String
                's' parameter s: String
                'i' parameter i: I32
                'a' let a: Base.String
                """);

        [Fact]
        public void MemberOfGenericType()
            => InlineSnapshot.Validate(Info("""
                                            let list = MutList[I32].Empty();
                                            |list.|Add(1);          // let list: MutList[I32]
                                                                    // fun MutList[I32].Add(item: I32)
                                            fun Wrap[T](b: UnsafeMutBuffer[T]) {
                                                |b;                 // parameter b: UnsafeMutBuffer[T]
                                            }
                                            """), """
                'list' let list: MutList[I32]
                'Add' fun MutList[I32].Add(item: I32)
                'b' parameter b: UnsafeMutBuffer[T]
                """);

        [Fact]
        public void InsideOwnType()
            => InlineSnapshot.Validate(Info("""
                                            struct Counter
                                            {
                                                pub Value: I32;
                                                pub fun Inc(): I32 = |Value + 1;       // field Value: I32   (no Counter. inside)
                                                pub fun Twice(): I32 = |Inc() + 1;     // fun Inc()
                                            }
                                            let c = Counter(Value = 1);
                                            c.|Value;               // field Counter.Value: I32
                                            """), """
                'Value' field Value: I32
                'Inc' fun Inc()
                'Value' field Counter.Value: I32
                """);
    }
}
