namespace Axl.Compiler.Mir.Analysis;

public sealed class FunctionAnalysis(MutMirFunction function)
{
    private ReachabilityResult? _reachability = null;
    
    public MutMirFunction Function { get; } = function;


    public bool IsReachable(BasicBlockId block)
    {
        if (_reachability is null || _reachability.CfgVersion != Function.CfgVersion)
            _reachability = Reachability.Analyze(Function);

        return _reachability.Reachable[block];
    }

    public bool IsReachable(MutBasicBlock block)
    {
        Guard.MustBe(block.MirFunction == Function);
        return IsReachable(block.Id);
    }
}