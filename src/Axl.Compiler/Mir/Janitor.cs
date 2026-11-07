using System.Diagnostics;

namespace Axl.Compiler.Mir;

/// <summary>
/// Simplifies constant branches and removes unreachable blocks.
/// </summary>
public sealed class Janitor
{
    private readonly HashSet<MutBasicBlock> _reachableBlocks = [];

    private Janitor()
    {

    }

    public static void Clean(MutMirFunction mirFunction)
    {
        var janitor = new Janitor();
        janitor.CleanFunction(mirFunction);
    }

    private void CleanFunction(MutMirFunction mirFunction)
    {
        CleanBlock(mirFunction.Entry);

        mirFunction.RemoveAllBasicBlocks(block => !_reachableBlocks.Contains(block));
    }

    private void CleanBlock(MutBasicBlock block)
    {
        if (!_reachableBlocks.Add(block))
            return;

        switch (block.Terminator)
        {
            case Terminator.Goto @goto:
                CleanBlock(@goto.Block);
                break;

            case Terminator.Branch branch:
                if (branch.Condition is Operand.Const { Value: true })
                {
                    CleanBlock(branch.True);
                    block.Terminator = new Terminator.Goto(branch.True, branch.Syntax);
                }
                else if (branch.Condition is Operand.Const { Value: false })
                {
                    CleanBlock(branch.False);
                    block.Terminator = new Terminator.Goto(branch.False, branch.Syntax);
                }
                else
                {
                    CleanBlock(branch.True);
                    CleanBlock(branch.False);
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