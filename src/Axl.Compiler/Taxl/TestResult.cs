using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Taxl;

public readonly record struct TestResult(IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool IsSuccessful => Diagnostics.Count == 0;
}