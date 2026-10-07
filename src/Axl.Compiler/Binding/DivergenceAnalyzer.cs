using System.Diagnostics;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding;

public static class DivergenceAnalyzer
{
    extension(BoundStmt boundStmt)
    {
        /// <summary>
        /// <c>True</c>, if statements after this one will not be
        /// executed and code instead flows somewhere else.
        /// </summary>
        public bool IsDiverging
        {
            get
            {
                if (boundStmt is BoundValue { Type: NeverTypeSymbol })
                    return true;
                
                return boundStmt switch
                {
                    BoundAssign boundAssign => boundAssign.Value.IsDiverging,
                    BoundBlock boundBlock => boundBlock.Stmts.Any(get_IsDiverging),
                    BoundBreak or BoundContinue => throw new UnreachableException(
                        "Loops are not handled, so break/continue are not reachable."),
                    BoundConst => false,
                    BoundError => false,
                    BoundReturn => true,
                    BoundVariable => false,
                    BoundSelf => false,
                    BoundWhile => false,
                    BoundCall boundCall => boundCall.Receiver?.IsDiverging == true ||
                                           boundCall.Arguments.Any(get_IsDiverging),
                    BoundAnd boundAnd => boundAnd.Left.IsDiverging,
                    BoundOr boundOr => boundOr.Left.IsDiverging,
                    BoundIfExpr boundIfExpr => boundIfExpr.Condition.IsDiverging ||
                                               (boundIfExpr.Then.IsDiverging && boundIfExpr.Else.IsDiverging),
                    BoundIfStmt boundIfStmt => boundIfStmt.Condition.IsDiverging ||
                                               (boundIfStmt.Then.IsDiverging && boundIfStmt.Else?.IsDiverging == true),
                    BoundStringExpr boundStringExpr => boundStringExpr.Parts.Any(get_IsDiverging),
                    BoundVarDecl boundVarDecl => boundVarDecl.Initializer.IsDiverging,
                    
                    BoundFieldInit boundFieldInit => boundFieldInit.Value.IsDiverging,
                    BoundStructInit boundStructInit => boundStructInit.FieldInits.Any(get_IsDiverging),
                    BoundFieldAccess boundFieldAccess => boundFieldAccess.Receiver.IsDiverging,
                };
            }
        }
    }
}