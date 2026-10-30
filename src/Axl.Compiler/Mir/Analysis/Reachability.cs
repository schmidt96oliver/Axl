using System.Collections.Frozen;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Mir.Analysis;

public sealed record ReachabilityResult(int CfgVersion, FrozenDictionary<BasicBlockId, bool> Reachable);

public sealed class Reachability
{
    private readonly Dictionary<BasicBlockId, bool> _reachable = [];

    private Reachability()
    {
    }

    public static ReachabilityResult Analyze(MutMirFunction function)
    {
        if (function.BasicBlocks.Count == 0)
            return new ReachabilityResult(function.CfgVersion, []);

        var cfgVersion = function.CfgVersion;
        
        var analyzer = new Reachability();
        foreach (var block in function.BasicBlocks)
            analyzer._reachable[block.Id] = false;

        analyzer.Visit(function.Entry);
        Debug.Assert(function.CfgVersion == cfgVersion, "CFG changed during reachability analysis.");
        
        return new ReachabilityResult(function.CfgVersion, analyzer._reachable.ToFrozenDictionary());
    }

    private void Visit(MutBasicBlock block)
    {
        if (_reachable[block.Id])
            return;
        _reachable[block.Id] = true;

        switch (block.Terminator)
        {
            case Terminator.Goto @goto:
                Visit(@goto.Block);
                break;
            case Terminator.Branch branch:
                if (branch.Condition is Operand.Const {Value: true })
                    Visit(branch.True);
                else if (branch.Condition is Operand.Const {Value: false})
                    Visit(branch.False);
                else
                {
                    Visit(branch.True);
                    Visit(branch.False);
                }
                
                break;
            
            case null:
            case Terminator.Return:
            case Terminator.MissingReturn:
            case Terminator.Unreachable:
                break;
            
            default:
                throw new UnreachableException();
        }
    }
}