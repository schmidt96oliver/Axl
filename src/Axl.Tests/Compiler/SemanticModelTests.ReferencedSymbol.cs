using System.Diagnostics;
using AxlCompiler;
using AxlCompiler.Diagnostics;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Testing;
using AxlCompiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler;

public partial class SemanticModelTests
{
    public sealed class ReferencedSymbol
    {
        /// <summary>
        /// Each input is its own file. Carets can be placed in any of them.
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
            var texts = carets
                .Select(c => compilation.SyntaxTrees[c.File].TokenAt(c.Caret).Parent as NameRefSyntax
                             ?? throw new InvalidOperationException("Caret is not on a name ref."))
                .Select(nameRef => $"\'{nameRef.Text}\' " + CaretInfo(compilation, nameRef));

            return string.Join("\n", texts);
        }

        private string CaretInfo(Compilation compilation, NameRefSyntax nameRef)
        {
            var symbolInfo = compilation.SemanticModel.TryReferencedSymbolFor(nameRef);
            return symbolInfo switch
            {
                null => "<NULL>",
                SymbolReference.Resolved(var symbol) => $"Resolved: {symbol.FullDisplayText}",
                SymbolReference.Unresolved(var candidates, var reason) => $"Unresolved ({reason}):\n   {
                    string.Join("\n   ", candidates.Select(c => c.FullDisplayText))}"
            };
        }

        [Theory]
        [Corpus]
        public void Corpus_AllNameRef_NonNullSymbol(string path)
        {
            var compilation = Compilation.From(TestFile.From(SourceText.LoadFile(path))).WithBaseLibrary();
            var nameRefs = compilation.SyntaxTrees
                .SelectMany(tree => tree.FileSyntax.Traverse()
                .OfType<NameRefSyntax>())
                .ToList();
            
            var wrongSymbols = nameRefs
                .Select(nameRef => (Ref: nameRef, Fail: CheckInfoAgainstNameRef(
                    compilation.SemanticModel.TryReferencedSymbolFor(nameRef),
                    nameRef)))
                .Where(pair => pair.Fail is not null)
                .Select(pair => $"\'{pair.Ref.Text}\' in \'{pair.Ref.Parent?.Location.Text.ToString().Replace("\n", " ") ?? ""}\':\n   {pair.Fail}")
                .ToList();
            if (wrongSymbols.Count > 0)
            {
                Assert.Fail($"$Invalid references:\n{ string.Join("\n", wrongSymbols) }");
            }
        }

        private string? CheckInfoAgainstNameRef(SymbolReference? info, NameRefSyntax nameRef)
        {
            if (info is null)
                return "GAP! No symbol info.";

            var symbols = info switch
            {
                SymbolReference.Resolved(var s) => [s],
                SymbolReference.Unresolved(var c, _) => c
            };
            var wrongName = symbols.Where(s => s.Name != nameRef.Text.ToString()).ToList();
            if (wrongName.Count > 0)
                return "Unexpected name on " + string.Join(", ", wrongName.Select(s => s.ShortDisplayText));

            if (info is SymbolReference.Unresolved(var candidates, var reason))
            {
                return (reason, candidates.Length) switch
                {
                    (UnresolvedSymbolReason.NotFound, > 0) => $"Unresolved {reason} but has candidates",
                    (UnresolvedSymbolReason.NotFound, 0) => null,
                    (UnresolvedSymbolReason.Ambiguous, < 2) => $"Unresolved {reason} but with less than 2 candidates",
                    (_, 0) => $"Unresolved {reason}, but no candidates",
                    _ => null
                };
            }
            
            return null;
        }


        // --- Locals and parameters

        [Fact]
        public void Locals()
            => InlineSnapshot.Validate(Info("""
                                            fun A[T](p: I32) {
                                                let a = p;
                                                |a;   // Resolved: let a
                                                |p;   // Resolved: parameter p
                                                |T;   // Resolved: type parameter T
                                            }
                                            """), """
                'a' Resolved: let a: Base.I32
                'p' Resolved: parameter p: Base.I32
                'T' Resolved: type parameter T
                """);

        [Fact]
        public void Locals_Shadowing()
            => InlineSnapshot.Validate(Info("""
                                            struct T;
                                            fun A[T](a: I32) {
                                                let a = "";
                                                |a;          // Resolved: the String local, not the parameter
                                                let b: |T;   // Resolved: the type parameter, not the struct
                                            }
                                            let c: I32 = 1;
                                            let c: String = "";
                                            let d = |c;      // Resolved: the String c
                                            """), """
                'a' Resolved: let a: Base.String
                'T' Resolved: type parameter T
                'c' Resolved: let c: Base.String
                """);

        // --- Types and namespaces

        [Fact]
        public void Types_InDeclarations()
            => InlineSnapshot.Validate(Info("""
                                            struct S[T] {
                                                A: |T;               // Resolved: type parameter T of S
                                                B: |I32;             // Resolved: Base.I32
                                                C: B|ase.I|32;       // Resolved: namespace Base, then Base.I32
                                            }
                                            fun F(p: |S[|I32]): |I32 = 1;   // Resolved: S[I32] (specialized), I32, I32
                                            """), """
                'T' Resolved: type parameter T
                'I32' Resolved: struct Base.I32
                'Base' Resolved: namespace Base
                'I32' Resolved: struct Base.I32
                'S' Resolved: struct S[Base.I32]
                'I32' Resolved: struct Base.I32
                'I32' Resolved: struct Base.I32
                """);

        [Fact]
        public void Types_NamespaceQualified()
            => InlineSnapshot.Validate(Info("""
                                            namespace A.B;
                                            struct Type;
                                            fun Test(p: |A.|B.|Type) {}   // Resolved: namespace A, namespace A.B, struct A.B.Type
                                            """), """
                'A' Resolved: namespace A
                'B' Resolved: namespace A.B
                'Type' Resolved: struct A.B.Type
                """);

        [Fact]
        public void Types_GenericArity()
            => InlineSnapshot.Validate(Info("""
                                            struct Box;
                                            struct Box[T];

                                            let a: |Box;             // Resolved: the non-generic Box
                                            let b: |Box[I32];        // Resolved: Box[I32]
                                            let c: |Box[I32, Bool];  // Unresolved (NoApplicableType): Box, Box[T]
                                            """), """
                'Box' Resolved: struct Box
                'Box' Resolved: struct Box[Base.I32]
                'Box' Unresolved (NoApplicableType):
                   struct Box
                   struct Box[T]
                """);

        [Fact]
        public void Types_GenericWithoutArguments()
            => InlineSnapshot.Validate(Info("""
                                            struct Box[T] { pub Value: T; }
                                            let a: |Box;     // Unresolved (NoApplicableType): Box[T], missing type arguments
                                            |Box.Value;      // Unresolved (NoApplicableType): Box[T], same through member access
                                            """), """
                'Box' Unresolved (NoApplicableType):
                   struct Box[T]
                'Box' Unresolved (NoApplicableType):
                   struct Box[T]
                """);

        [Fact]
        public void Types_Subscripts()
            => InlineSnapshot.Validate(Info("""
                                            let x = 1;
                                            |x[I32];         // Resolved: let x, only the subscript is wrong
                                            |I32[Bool];      // Resolved: Base.I32, NotGeneric is about the subscript
                                            Base[|I32];      // Resolved: Base.I32
                                            Nope[|I32];      // Resolved: Base.I32, the type argument is fine
                                            """), """
                'x' Resolved: let x: Base.I32
                'I32' Resolved: struct Base.I32
                'I32' Resolved: struct Base.I32
                'I32' Resolved: struct Base.I32
                """);


        [Fact]
        public void Types_ThroughUsings()
            => InlineSnapshot.Validate(Info(
                """
                using |Lib;              // Resolved: namespace Lib
                using |Lib.|Inner;       // Resolved: namespace Lib, namespace Lib.Inner
                using |Nope;             // Unresolved (NotFound)

                let a = |Thing(V = 1);   // Resolved: struct Lib.Thing, found through using Lib
                let b: |Deep;            // Resolved: struct Lib.Inner.Deep, found through using Lib.Inner
                let c: |Same;            // Unresolved (Ambiguous): Lib.Same, Lib.Inner.Same
                let d: |Hidden;          // Unresolved (NotAccessible): Lib.Hidden
                """,
                """
                namespace Lib;
                pub struct Thing { pub V: I32; }
                pub struct Same;
                struct Hidden;
                """,
                """
                namespace Lib.Inner;
                pub struct Deep;
                pub struct Same;
                """), """
                'Lib' Resolved: namespace Lib
                'Lib' Resolved: namespace Lib
                'Inner' Resolved: namespace Lib.Inner
                'Nope' Unresolved (NotFound):

                'Thing' Resolved: struct Lib.Thing
                'Deep' Resolved: struct Lib.Inner.Deep
                'Same' Unresolved (Ambiguous):
                   struct Lib.Same
                   struct Lib.Inner.Same
                'Hidden' Unresolved (NotAccessible):
                   struct Lib.Hidden
                """);


        // --- Struct initialization

        [Fact]
        public void StructInit_Inference()
            => InlineSnapshot.Validate(Info("""
                                            struct Box { pub Val1: I32; }
                                            struct Box[T] { pub Value: T; }
                                            struct Box[T, U] { pub Value1: T; pub Value2: U; }
                                            let a = |Box(Val1 = 1);                   // Resolved: Box
                                            let b = |Box(Value = 1);                  // Resolved: Box[I32]
                                            let c = |Box(Value1 = 1, Value2 = "");    // Resolved: Box[I32, String]
                                            let d = |Box(Value1 = 1);                 // Unresolved (NoApplicableType): all three
                                            let e = |Box();                           // Unresolved (NoApplicableType): all three
                                            """), """
                'Box' Resolved: struct Box
                'Box' Resolved: struct Box[Base.I32]
                'Box' Resolved: struct Box[Base.I32, Base.String]
                'Box' Unresolved (NoApplicableType):
                   struct Box
                   struct Box[T]
                   struct Box[T, U]
                'Box' Unresolved (NoApplicableType):
                   struct Box
                   struct Box[T]
                   struct Box[T, U]
                """);
        [Fact]
        public void StructInit_FieldNames()
            => InlineSnapshot.Validate(Info("""
                                            struct Box { pub Val1: I32; }
                                            let a = Box(Va|l1 = 1);
                                            """), "'Val1' Resolved: field Box.Val1: Base.I32");


        // --- Calls

        [Fact]
        public void Calls_Overloads()
            => InlineSnapshot.Validate(Info("""
                                            fun Over(a: I32) {}
                                            fun Over(a: String) {}
                                            |Over(1);      // Resolved: Over(a: I32)
                                            |Over("");     // Resolved: Over(a: String)
                                            |Over(true);   // Unresolved (NoApplicableOverload): both
                                            """), """
                'Over' Resolved: fun Over(a: Base.I32)
                'Over' Resolved: fun Over(a: Base.String)
                'Over' Unresolved (NoApplicableOverload):
                   fun Over(a: Base.I32)
                   fun Over(a: Base.String)
                """);

        [Fact]
        public void Calls_MutOverloads()
            => InlineSnapshot.Validate(Info("""
                                            fun M(a: I32) {}
                                            fun M(mut a: I32) {}
                                            var v = 1;
                                            |M(v);         // Resolved: M(a: I32)
                                            |M(mut v);     // Resolved: M(mut a: I32)
                                            """), """
                'M' Resolved: fun M(a: Base.I32)
                'M' Resolved: fun M(a: mut Base.I32)
                """);

        [Fact]
        public void Calls_GenericFun()
            => InlineSnapshot.Validate(Info("""
                                            fun Id[T](t: T): T = t;
                                            |Id(1);              // Resolved: Id[I32], inferred
                                            |Id[I32](1);         // Resolved: Id[I32], explicit
                                            |Id[I32, I32](1);    // Unresolved (NoApplicableOverload): Id[T]
                                            |Id();               // Unresolved (NoApplicableOverload): Id[T], nothing to infer from
                                            """), """
                'Id' Resolved: fun Id[Base.I32](t: Base.I32)
                'Id' Resolved: fun Id[Base.I32](t: Base.I32)
                'Id' Unresolved (NoApplicableOverload):
                   fun Id[T](t: T)
                'Id' Unresolved (NoApplicableOverload):
                   fun Id[T](t: T)
                """);

        [Fact]
        public void Calls_FunAsValue()
            => InlineSnapshot.Validate(Info("""
                                            fun F() {}
                                            let f = |F;    // Unresolved (NotUsable): F, a fun used as a value
                                            """), """
                'F' Unresolved (NotUsable):
                   fun F()
                """);


        // --- Members and receivers

        [Fact]
        public void Members_ImplicitSelf()
            => InlineSnapshot.Validate(Info("""
                                            struct Q {
                                                X: I32;
                                                fun Len(): I32 = 1;
                                                fun F() { |X; |Len(); }          // Resolved: field X, fun Len
                                                static fun G() { |X; |Len(); }   // Unresolved (NotUsable): X, Len, no self here
                                            }
                                            """), """
                'X' Resolved: field Q.X: Base.I32
                'Len' Resolved: fun Q.Len()
                'X' Unresolved (NotUsable):
                   field Q.X: Base.I32
                'Len' Unresolved (NoApplicableOverload):
                   fun Q.Len()
                """);

        [Fact]
        public void Members_Receivers()
            => InlineSnapshot.Validate(Info("""
                                            struct P {
                                                pub X: I32;
                                                pub fun Len(): I32 = X;
                                                pub mut fun Bump() { X = X + 1; }
                                                pub static fun Make(): P = P(X = 1);
                                            }
                                            let p = P.Make();
                                            var q = P.Make();
                                            |P.|Make();    // Resolved: struct P, fun Make
                                            p.|Len();      // Resolved: fun Len
                                            q.|Bump();     // Resolved: fun Bump
                                            p.|Make();     // Unresolved (NotUsable): Make, static through instance
                                            P.|Len();      // Unresolved (NotUsable): Len, instance through type
                                            p.|Bump();     // Unresolved (NotUsable): Bump, p is not mutable
                                            P.|X;          // Unresolved (NotUsable): X, instance field through type
                                            """), """
                'P' Resolved: struct P
                'Make' Resolved: fun P.Make()
                'Len' Resolved: fun P.Len()
                'Bump' Resolved: fun P.Bump()
                'Make' Unresolved (NoApplicableOverload):
                   fun P.Make()
                'Len' Unresolved (NoApplicableOverload):
                   fun P.Len()
                'Bump' Unresolved (NoApplicableOverload):
                   fun P.Bump()
                'X' Unresolved (NotUsable):
                   field P.X: Base.I32
                """); //TODO!

        [Fact]
        public void Members_Specialized()
            => InlineSnapshot.Validate(Info("""
                                            struct Box[T] {
                                                pub Value: T;
                                                fun Get(): |T = |Value;                    // Resolved: T, field Box[T].Value
                                                fun Copy(): |Box[T] = Box(Value = Value);  // Resolved: Box[T]
                                            }
                                            let b = Box(Value = 1);
                                            b.|Value;      // Resolved: field Box[I32].Value
                                            """), """
                'T' Resolved: type parameter T
                'Value' Resolved: field Box[T].Value: T
                'Box' Resolved: struct Box[T]
                'Value' Resolved: field Box[Base.I32].Value: Base.I32
                """);

        [Fact]
        public void Members_ColorColor()
            => InlineSnapshot.Validate(Info("""
                                            struct Color { pub R: I32; pub static fun Red(): Color = Color(R = 1); }
                                            struct S {
                                                pub Color: Color;
                                                fun F() { |Color.|R; }             // Resolved: field Color (value), field R
                                                fun G() { |Color.|Red(); }         // Resolved: struct Color (static access), fun Red
                                                fun H() { |Color; }                // Resolved: field Color (implicit self)
                                                static fun K() { |Color.|Red(); }  // Resolved: struct Color, no self to read a field from
                                                fun L(c: |Color) {}                // Resolved: struct Color
                                            }
                                            """), """
                'Color' Resolved: field S.Color: Color
                'R' Resolved: field Color.R: Base.I32
                'Color' Resolved: struct Color
                'Red' Resolved: fun Color.Red()
                'Color' Resolved: field S.Color: Color
                'Color' Resolved: struct Color
                'Red' Resolved: fun Color.Red()
                'Color' Resolved: struct Color
                """);


        // --- Lookup errors

        [Fact]
        public void Errors_Lookup()
            => InlineSnapshot.Validate(Info("""
                                            struct Test { Secret: I32; }
                                            struct T;
                                            struct T;
                                            let t: Test = Test(Secret = 1);
                                            Bl|aa;           // Unresolved (NotFound)
                                            Base.Blla|aa;    // Unresolved (NotFound)
                                            t.Se|cret;       // Unresolved (NotAccessible): Secret
                                            let u: |T;       // Unresolved (Ambiguous): T, T
                                            |Nope.|X;        // Unresolved (NotFound) twice, X only cascades
                                            """), """
                'Blaa' Unresolved (NotFound):

                'Bllaaa' Unresolved (NotFound):

                'Secret' Unresolved (NotAccessible):
                   field Test.Secret: Base.I32
                'T' Unresolved (Ambiguous):
                   struct T
                   struct T
                'Nope' Unresolved (NotFound):

                'X' Unresolved (NotFound):

                """);

        [Fact]
        public void Errors_MixedKinds()
            => InlineSnapshot.Validate(Info("""
                                            struct F;
                                            fun F() {}
                                            |F();            // Unresolved (Ambiguous): struct F, fun F
                                            """), """
                'F' Unresolved (Ambiguous):
                   struct F
                   fun F()
                """);

        [Fact]
        public void Errors_WrongKind()
            => InlineSnapshot.Validate(Info("""
                                            let x = 1;
                                            let a: |x = 1;   // Unresolved (NotUsable): x, not a type
                                            |x();            // Unresolved (NotUsable): x, not callable
                                            """), """
                'x' Unresolved (NotUsable):
                   let x: Base.I32
                'x' Unresolved (NotUsable):
                   let x: Base.I32
                """);

        [Fact]
        public void Errors_UsingNotANamespace()
            => InlineSnapshot.Validate(Info("""
                                            using |Base.|I32;   // Resolved: namespace Base; Unresolved (NotUsable): I32, not a namespace
                                            """), """
                'Base' Resolved: namespace Base
                'I32' Unresolved (NotUsable):
                   struct Base.I32
                """);


        // --- Mut returns

        [Fact]
        public void MutReturn_Mismatch()
            => InlineSnapshot.Validate(Info("""
                                            fun A(mut a: I32): mut I32 = |a;      // Resolved: parameter a, only the missing mut is wrong
                                            fun B(mut b: I32): I32 = mut |b;      // Resolved: parameter b, only the unexpected mut is wrong
                                            """), """
                'a' Resolved: parameter a: mut Base.I32
                'b' Resolved: parameter b: mut Base.I32
                """);
    }
}