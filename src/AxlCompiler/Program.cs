using AxlCompiler;
using AxlCompiler.Diagnostics;
using AxlCompiler.Mir;
using AxlCompiler.Testing;
using AxlCompiler.Text;

var testDirectory = Path.Combine("..", "..", "..", "..", "src", "AxlCompiler");
var testFilePath = Path.Combine(testDirectory, "test.taxl");

var sourceText = SourceText.LoadFile(testFilePath);
var testFile = Path.GetExtension(testFilePath) is ".taxl"
    ? TestFile.From(sourceText)
    : null;
var compilation = Compilation
    .From(testFile is not null
        ? testFile.Fragments.Select(f => f.SourceText)
        : [sourceText])
    .WithBaseLibrary();

if (compilation.Scripts.Length != 1)
{
    Console.WriteLine($"Compilation has {compilation.SyntaxTrees.Length} scripts, but can only run 1.");
    return;
}

var result = compilation.Compile(compilation.Scripts[0]);

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine(testFile is not null
        ? diagnostic.GetDisplayString(testDirectory,
            l => testFile.TryMapToTestFile(l) ?? l)
        : diagnostic.GetDisplayString(testDirectory));
}

if (!result.IsSuccessful)
    return;

Console.WriteLine("---------------");
Console.WriteLine("--- GENERIC ---");
Console.WriteLine("---------------");

foreach (var fun in compilation.AllFuns.Select(compilation.GetLoweredFunBody))
{
    Console.WriteLine(fun.MirFunction.Print());
    Console.WriteLine();
}

Console.WriteLine("---------------");
Console.WriteLine("--- MONOMOR ---");
Console.WriteLine("---------------");

foreach (var fun in result.Program!.MirFunctions.Where(f => !f.DisplayName.StartsWith("Base")).OrderBy(f => f.DisplayName, StringComparer.Ordinal))
{
    Console.WriteLine(fun.Print());
    Console.WriteLine();
}

Console.WriteLine("----------------");
Miri.Run(result.Program!, Console.Out);