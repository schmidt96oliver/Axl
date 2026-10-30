using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Mir.Analysis;

public static class ReturnCheck
{
    public static void Check(MutMirFunction function, ReachabilityResult reachability, DiagnosticBag diagnosticBag)
    {
        Debug.Assert(function.Symbol.OriginalDefinition == function.Symbol, "Analysis must run on the definition funs.");
        
        var missingReturnReachable = function.BasicBlocks
            .Any(b => reachability.Reachable[b.Id] && b.Terminator is Terminator.MissingReturn);
        
        if (missingReturnReachable)
        {
            if (function.Symbol.OriginalDefinition.DeclarationSyntax is null)
            {
                throw new ArgumentException("The given function is synthetic and not all code paths return a value.",
                    nameof(function));
            }
            
            diagnosticBag.ReportError(new Diagnostic.MissingReturn(function.Symbol.OriginalDefinition));
        }
    }
}