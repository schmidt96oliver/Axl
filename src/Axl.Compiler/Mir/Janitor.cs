using System.Diagnostics;

namespace Axl.Compiler.Mir;

/// <summary>
/// Performs basic cleanup: Removes unreachable blocks, simplifies constant branches and removes locals
/// that are never seen.
/// </summary>
public sealed class Janitor
{
    private readonly HashSet<MutBasicBlock> _reachableBlocks = [];
    private readonly HashSet<Place.Local> _usedLocals = [];

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
        // Return place and parameter must not be removed
        _usedLocals.Add(mirFunction.ReturnPlace);
        foreach (var param in mirFunction.ParameterPlaces)
            _usedLocals.Add(param);
        
        VisitBlock(mirFunction.Entry);

        mirFunction.RemoveAllBasicBlocks(block => !_reachableBlocks.Contains(block));
        mirFunction.RemoveAllLocals(local => !_usedLocals.Contains(local));
    }

    private void VisitBlock(MutBasicBlock block)
    {
        if (!_reachableBlocks.Add(block))
            return;

        foreach (var instruction in block.Instructions)
        foreach (var local in GetUsedLocals(instruction))
            _usedLocals.Add(local);
        
        switch (block.Terminator)
        {
            case Terminator.Goto @goto:
                VisitBlock(@goto.Block);
                break;

            case Terminator.Branch branch:
                if (branch.Condition is Operand.Const { Value: true })
                {
                    VisitBlock(branch.True);
                    block.Terminator = new Terminator.Goto(branch.True, branch.Syntax);
                }
                else if (branch.Condition is Operand.Const { Value: false })
                {
                    VisitBlock(branch.False);
                    block.Terminator = new Terminator.Goto(branch.False, branch.Syntax);
                }
                else
                {
                    foreach (var usedLocal in GetUsedLocals(branch.Condition))
                        _usedLocals.Add(usedLocal);
                    
                    VisitBlock(branch.True);
                    VisitBlock(branch.False);
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

    private IEnumerable<Place.Local> GetUsedLocals(Instruction instruction)
        => [..GetUsedLocals(instruction.Target), .. instruction.Operands.SelectMany(GetUsedLocals)];

    private IEnumerable<Place.Local> GetUsedLocals(Operand operand) => operand switch
    {
        Operand.Copy copy => GetUsedLocals(copy.Place),
        Operand.Ref @ref => GetUsedLocals(@ref.Place),
        Operand.Const or Operand.Empty => [],
        Operand.Unreachable or Operand.Error => [],
    };

    private IEnumerable<Place.Local> GetUsedLocals(Place place) => place switch
    {
        Place.Local local => [local],
        Place.Deref deref => GetUsedLocals(deref.Reference),
        Place.Field field => GetUsedLocals(field.Receiver),
        Place.Discard or Place.Error => [],
    };
}