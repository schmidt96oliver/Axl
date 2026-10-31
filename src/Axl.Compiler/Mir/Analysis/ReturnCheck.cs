using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir.Analysis;

public static class ReturnCheck
{
    public static void Check(FunDefinitionSymbol funDefinition, MutMirFunction mirFunction, ReachabilityResult reachability, DiagnosticBag diagnosticBag)
    {
        var missingReturnReachable = mirFunction.BasicBlocks
            .Any(b => reachability.Reachable[b.Id] && b.Terminator is Terminator.MissingReturn);
        
        if (missingReturnReachable)
        {
            if (funDefinition.DeclarationSyntax is null)
            {
                throw new ArgumentException("The given function is synthetic and not all code paths return a value.",
                    nameof(mirFunction));
            }
            
            diagnosticBag.ReportError(new Diagnostic.MissingReturn(funDefinition));
        }
    }
}