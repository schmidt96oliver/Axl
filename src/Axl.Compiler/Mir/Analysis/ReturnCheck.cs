using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir.Analysis;

public static class ReturnCheck
{
    /// <summary>
    /// Expects that <see cref="Janitor"/> has cleaned up <paramref name="mirFunction"/> before use.
    /// </summary>
    public static void Check(FunDefinitionSymbol funDefinition, MutMirFunction mirFunction, DiagnosticBag diagnosticBag)
    {
        // Suppress missing return if return type is an error.
        if (mirFunction.ReturnPlace.Type is ErrorTypeSymbol)
            return;
        
        // Note: Cleanup after lowering removes unreachable blocks.
        
        var missingReturnReachable = mirFunction.BasicBlocks.Any(b => b.Terminator is Terminator.MissingReturn);
        
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