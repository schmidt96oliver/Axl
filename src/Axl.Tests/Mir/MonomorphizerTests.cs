using Axl.Compiler;
using Axl.Compiler.Symbols;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Mir;

public sealed class MonomorphizerTests
{
    private string Signatures(string input)
    {
        var compilation = Compilation.From(SourceText.From(input)).AddBaseLibrary();
        var program = compilation.Compile(compilation.Scripts[0]);
        var mirFuns = program.MirFunctions.OrderBy(f => f.DisplayName, StringComparer.InvariantCulture);
        return string.Join('\n', mirFuns.Select(f => 
            $"{ f.DisplayName }({ string.Join(", ", f.ParameterPlaces.Select(p => $"{p.Id}: {p.Type.FullName}")) }): {f.ReturnPlace.Type.FullName}"));
    }

    [Fact]
    public void NeverInstantiated()
        => InlineSnapshot.Validate(Signatures("fun A[T](t: T): T = t;"), "(): Base.Unit");

    [Fact]
    public void MultipleCalls()
        => InlineSnapshot.Validate(Signatures("""
                                              A[I32]();
                                              A[I32]();
                                              A[I32]();
                                              A[I32]();
                                              
                                              fun A[T]() { A[T](); }
                                              """), """
            (): Base.Unit
            A[Base.I32](): Base.Unit
            """);
    [Fact]
    public void Cyclic()
        => InlineSnapshot.Validate(Signatures("""
                                              A[I32]();
                                              A[String]();
                                              
                                              fun A[T]() { A[T](); }
                                              """), """
            (): Base.Unit
            A[Base.I32](): Base.Unit
            A[Base.String](): Base.Unit
            """);
    [Fact]
    public void Mixed_1()
        => InlineSnapshot.Validate(Signatures("""
                                              A[I32]();
                                              A[String]();
                                              
                                              fun A[T]() { A[T](); A[I32](); A[String](); }
                                              """), """
            (): Base.Unit
            A[Base.I32](): Base.Unit
            A[Base.String](): Base.Unit
            """);
    [Fact]
    public void Mixed_2()
        => InlineSnapshot.Validate(Signatures("""
                                              A[I32]();
                                              A[Bool]();
                                              
                                              fun A[T]() { B[String](); }
                                              fun B[T]() { }
                                              """), """
            (): Base.Unit
            A[Base.Bool](): Base.Unit
            A[Base.I32](): Base.Unit
            B[Base.String](): Base.Unit
            """);
    [Fact]
    public void SwappedParams()
        => InlineSnapshot.Validate(Signatures("""
                                              A[I32, String]();
                                              
                                              fun A[T1, T2]() { B[T2, T1](); }
                                              fun B[T1, T2]() { A[T2, T1](); }
                                              """), """
            (): Base.Unit
            A[Base.I32, Base.String](): Base.Unit
            B[Base.String, Base.I32](): Base.Unit
            """);
}