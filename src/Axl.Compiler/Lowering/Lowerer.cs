using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Lowering;

public class Lowerer
{
    private readonly Compilation _compilation;
    private readonly FunSymbol _funSymbol;
    private readonly MirFunction _function;
    private BasicBlock? _currentBlock;
    private Dictionary<VariableSymbol, Place.Local> _variables = [];

    private Stack<(BasicBlock Break, BasicBlock Continue)> _loopLabels = [];

    private TypeSymbol Unit => _compilation.BaseLibrary.Unit;
    private TypeSymbol Bool => _compilation.BaseLibrary.Bool;
    
    private Lowerer(Compilation compilation, FunSymbol funSymbol)
    {
        _compilation = compilation;
        _funSymbol = funSymbol;
        
        _function = new MirFunction(funSymbol);
        foreach (var param in _function.Parameters)
            _variables[param] = _function.GetParameterPlace(param);
        
        _currentBlock = _function.Entry;
    }

    public static MirFunction Lower(FunSymbol fun, Compilation compilation)
    {
        if (fun.Body is not BoundBlock body)
            throw new ArgumentException("Cannot lower funs without blocks.", nameof(fun));
        
        var lowerer = new Lowerer(compilation, fun);
        lowerer.LowerStmt(body);
        
        if (lowerer._currentBlock is not null &&
            lowerer._currentBlock.Terminator is null)
        {
            lowerer.Terminate(new Terminator.Unreachable());
        }

        return lowerer._function;
    }


    private void Emit(Instruction instruction)
    {
        _currentBlock ??= _function.NewBasicBlock();
        _currentBlock.Instructions.Add(instruction);
    }

    private void Terminate(Terminator terminator)
    {
        _currentBlock ??= _function.NewBasicBlock();
        _currentBlock.Terminator = terminator;
        _currentBlock = null;
    }

    private void Enter(BasicBlock block)
    {
        Debug.Assert(_currentBlock is null, "Current block must be terminated.");
        _currentBlock = block;
    }
    
    
    public void LowerStmt(BoundStmt stmt)
    {
        switch (stmt)
        {
            case BoundValue value:
                LowerInto(value, Place.Discard.Instance);
                break;
            
            case BoundIfStmt ifStmt:
                LowerIfStmt(ifStmt);
                break;
            
            case BoundVarDecl varDecl:
                LowerVarDecl(varDecl);
                break;
            
            case BoundWhile @while:
                LowerWhile(@while);
                break;
            
            default:
                throw new UnreachableException();
        }
    }

    private void LowerVarDecl(BoundVarDecl varDecl)
    {
        if (varDecl.Variable.Type is NeverTypeSymbol)
        {
            // Never does not create a variable that is written to.
            LowerInto(varDecl.Initializer, Place.Discard.Instance);
            return;
        }
        
        if (!_variables.TryGetValue(varDecl.Variable, out var local))
        {
            local = _function.NewLocal(varDecl.Variable.Type, varDecl.Syntax);
            _variables[varDecl.Variable] = local;
        }
        
        LowerInto(varDecl.Initializer, local);
    }
    
    private void LowerWhile(BoundWhile @while)
    {
        var conditionBlock = _function.NewBasicBlock("while condition");
        var blockBlock = _function.NewBasicBlock("while body");
        var joinBlock = _function.NewBasicBlock("while join");
        
        Terminate(new Terminator.Goto(conditionBlock));
        
        Enter(conditionBlock);
        Terminate(new Terminator.Branch(LowerOperand(@while.Condition), True: blockBlock, False: joinBlock));
        
        Enter(blockBlock);
        
        _loopLabels.Push((Break: joinBlock, Continue: conditionBlock));
        LowerStmt(@while.Body);
        _loopLabels.Pop();
        
        Terminate(new Terminator.Goto(conditionBlock));
        Enter(joinBlock);
    }

    private void LowerIfStmt(BoundIfStmt ifStmt)
    {
        // if (condition) A else B

        //    _0 = condition
        //    branch _0 true false
        // true: 
        //    _target = A
        //    goto join
        // false:
        //    _target = B
        //    goto join
        // join:
        //    ...

        var trueBlock = _function.NewBasicBlock("if true");
        var joinBlock = _function.NewBasicBlock("if join");
        var falseBlock = ifStmt.Else is not null ? _function.NewBasicBlock("if false") : joinBlock;

        Terminate(new Terminator.Branch(
                LowerOperand(ifStmt.Condition), 
                trueBlock, 
                falseBlock, 
                ifStmt.Syntax));
        
        Enter(trueBlock);
        LowerStmt(ifStmt.Then);
        Terminate(new Terminator.Goto(joinBlock));
        
        Enter(falseBlock);

        if (ifStmt.Else is not null)
        {
            LowerStmt(ifStmt.Else);
            Terminate(new Terminator.Goto(joinBlock));
            
            Enter(joinBlock);
        }
    }


    private Place LowerPlace(BoundValue value)
    {
        switch (value)
        {
            case BoundVariable variable:
            {
                Place place = _variables[variable.Variable];
                if (place.Type is RefTypeSymbol)
                    place = new Place.Deref(place);
                return place;
            }
            case BoundField field:
                return new Place.Field(LowerPlace(field.Receiver), field.Field, field.Syntax);
            default:
                var temp = _function.NewLocal(value.Type);
                LowerInto(value, temp);
                return temp;
        }
    }

    private Operand LowerOperand(BoundValue value) => value switch
    {
        BoundBlock boundBlock => LowerBlock(boundBlock),

        BoundVariable boundVariable => new Operand.Copy(LowerPlace(boundVariable), boundVariable.Syntax),
        BoundConst boundConst => new Operand.Const(boundConst.Value, boundConst.Type, boundConst.Syntax),
        BoundField boundField => new Operand.Copy(LowerPlace(boundField), boundField.Syntax),

        BoundIfExpr boundIfExpr => LowerIfExpr(boundIfExpr, _function.NewLocal(boundIfExpr.Type)),

        BoundLogicAnd boundLogicAnd => LowerLogicOperator(boundLogicAnd, _function.NewLocal(boundLogicAnd.Type)),
        BoundLogicOr boundLogicOr => LowerLogicOperator(boundLogicOr, _function.NewLocal(boundLogicOr.Type)),
        BoundAssign boundAssign => LowerAssign(boundAssign),

        BoundBreak => LowerBreak(),
        BoundReturn boundReturn => LowerReturn(boundReturn),
        BoundContinue => LowerContinue(),

        BoundCall boundCall => LowerCall(boundCall, _function.NewLocal(boundCall.Type)),
        BoundStructInit boundStructInit => LowerStructInit(boundStructInit, _function.NewLocal(boundStructInit.Type)),

        BoundError or BoundFieldInit or BoundValueOrTypeRef => throw new UnreachableException(),
    };
    
    private Operand LowerInto(BoundValue value, Place target)
    {
        switch (value)
        {
            case BoundIfExpr ifExpr:
                return LowerIfExpr(ifExpr, target);
            case BoundLogicAnd logicAnd:
                return LowerLogicOperator(logicAnd, target);
            case BoundLogicOr logicOr:
                return LowerLogicOperator(logicOr, target);
            case BoundCall call:
                return LowerCall(call, target);
            case BoundStructInit structInit:
                return LowerStructInit(structInit, target);
        }
        
        // All others need to be assigned to the target.
        
        var operand = LowerOperand(value);
        if (operand is not Operand.Unreachable && target is not Place.Discard)
            Emit(new Instruction.Assign(target, operand, value.Syntax));
        return new Operand.Copy(target);
    }
    
    
    private Operand LowerBlock(BoundBlock block)
    {
        foreach (var stmt in block.Stmts)
            LowerStmt(stmt);

        return new Operand.Empty(Unit);
    }

    private Operand LowerAssign(BoundAssign assign)
    {
        LowerInto(assign.Value, LowerPlace(assign.Target));
        return new Operand.Empty(Unit);
    }

    
    private Operand LowerCall(BoundCall call, Place target)
    {
        var arguments = LowerArguments(call.ReceiverAndArguments, call.Fun.SelfAndParameters);
        
        if (call.Fun.Body is Intrinsic { IsVmCall: true } vmIntrinsic)
            Emit(new Instruction.VmCall(vmIntrinsic, target, arguments, call.Syntax));
        else if (call.Fun.Body is Intrinsic intrinsic)
            Emit(new Instruction.Intrinsic(intrinsic, target, arguments, call.Syntax));
        else
            Emit(new Instruction.Call(call.Fun, target, arguments, call.Syntax));

        return new Operand.Copy(target);
    }

    private ImmutableArray<Operand> LowerArguments(ImmutableArray<BoundValue> values,
        ImmutableArray<ParameterSymbol> parameters)
    {
        Debug.Assert(values.Length == parameters.Length);

        // If any argument might have a side effect, we need to lower
        // every argument into a temp to respect evaluation order.
        // Without side effects, this is not necessary.

        var mightHaveSideEffect = values.Any(MightHaveSideEffects);
        var arguments = ImmutableArray.CreateBuilder<Operand>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            if (parameters[i].IsMut)
                arguments.Add(new Operand.Ref(LowerPlace(values[i])));
            else if (values[i] is BoundConst || !mightHaveSideEffect)
                arguments.Add(LowerOperand(values[i]));
            else
                arguments.Add(LowerInto(values[i], _function.NewLocal(values[i].Type)));
        }

        return arguments.MoveToImmutable();
    }

    private static bool MightHaveSideEffects(BoundValue value) => value switch
    {
        BoundAssign => true,
        BoundBlock => true,
        BoundBreak => true,
        BoundCall => true,
        BoundContinue => true,
        BoundReturn => true,

        BoundIfExpr => true, // Lowers to temp anyway
        BoundStructInit => true, // Lowers to temp anyway

        BoundConst => false,
        BoundVariable => false,
        BoundError => false,
        BoundField => false,
        BoundLogicAnd boundLogicAnd => MightHaveSideEffects(boundLogicAnd.Left) ||
                                       MightHaveSideEffects(boundLogicAnd.Right),
        BoundLogicOr boundLogicOr => MightHaveSideEffects(boundLogicOr.Left) ||
                                     MightHaveSideEffects(boundLogicOr.Right),

        BoundValueOrTypeRef => throw new UnreachableException(),
        BoundFieldInit => throw new UnreachableException(),
    };


    private Operand LowerStructInit(BoundStructInit structInit, Place target)
    {
        var fields = Enumerable.ToDictionary(structInit.FieldInits, boundFieldInit => 
            boundFieldInit.Field, boundFieldInit => LowerOperand(boundFieldInit.Value));

        Emit(new Instruction.StructInit(structInit.Struct, target, fields.ToFrozenDictionary(), structInit.Syntax));
        return new Operand.Copy(target);
    }

    
    private Operand LowerIfExpr(BoundIfExpr ifExpr, Place target)
    {
        var trueBlock = _function.NewBasicBlock("if true");
        var falseBlock = _function.NewBasicBlock("if false");
        var joinBlock = _function.NewBasicBlock("if join");
        
        Terminate(new Terminator.Branch(LowerOperand(ifExpr.Condition), 
                trueBlock, 
                falseBlock, 
                ifExpr.Syntax));
        
        Enter(trueBlock);
        LowerInto(ifExpr.Then, target);
        Terminate(new Terminator.Goto(joinBlock));
        
        Enter(falseBlock);
        LowerInto(ifExpr.Else, target);
        Terminate(new Terminator.Goto(joinBlock));
        
        Enter(joinBlock);
        return new Operand.Copy(target);
    }

    private Operand LowerLogicOperator(BoundValue logic, Place target)
    {
        var (boundLeft, boundRight) = logic switch
        {
            BoundLogicAnd a => (a.Left, a.Right),
            BoundLogicOr o => (o.Left, o.Right),
            _ => throw new ArgumentException($"{nameof(logic)} is not a logic operator.", nameof(logic))
        };

        var assignLeftBlock = _function.NewBasicBlock();
        var assignRightBlock = _function.NewBasicBlock();
        var joinBlock = _function.NewBasicBlock();

        var left = LowerInto(boundLeft, _function.NewLocal(boundLeft.Type));
        Terminate(new Terminator.Branch(
                left, 
                True: logic is BoundLogicAnd ? assignRightBlock : assignLeftBlock, 
                False: logic is BoundLogicAnd ? assignLeftBlock : assignRightBlock, 
                logic.Syntax));
        
        Enter(assignLeftBlock);
        Emit(new Instruction.Assign(target, 
            new Operand.Const(logic is not BoundLogicAnd, Bool)));
        Terminate(new Terminator.Goto(joinBlock));
        
        Enter(assignRightBlock);
        LowerInto(boundRight, target);
        Terminate(new Terminator.Goto(joinBlock));
        
        Enter(joinBlock);
        return new Operand.Copy(target);
    }

    private Operand LowerBreak()
    {
        Debug.Assert(_loopLabels.Count > 0);
        Terminate(new Terminator.Goto(_loopLabels.Peek().Break));
        return Operand.Unreachable.Instance;
    }

    private Operand LowerContinue()
    {
        Debug.Assert(_loopLabels.Count > 0);
        Terminate(new Terminator.Goto(_loopLabels.Peek().Continue));
        return Operand.Unreachable.Instance;
    }

    private Operand LowerReturn(BoundReturn @return)
    {
        var value = @return.Value is not null
            ? LowerOperand(@return.Value)
            : new Operand.Empty(Unit);

        if (value is not Operand.Unreachable)
            Terminate(new Terminator.Return(value, @return.Syntax));
        return Operand.Unreachable.Instance;
    }
}