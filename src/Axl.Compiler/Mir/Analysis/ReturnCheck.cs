using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Mir.Analysis;

public static class ReturnCheck
{
    public static void Check(MutMirFunction function, DiagnosticBag diagnosticBag)
    {
        if (function.Symbol.ReturnType == function.Compilation.BaseLibrary.Unit)
            return;

        var reachableTerminators = function.BasicBlocks
            .Where(function.Analysis.IsReachable)
            .Select(b => b.Terminator)
            .ToImmutableArray();
        if (reachableTerminators.OfType<Terminator.MissingReturn>().FirstOrDefault() is { } missingReturn)
        {
            diagnosticBag.ReportError(new Diagnostic.MissingReturn(missingReturn.Syntax!));
        }
    }
}