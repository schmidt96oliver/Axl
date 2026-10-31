using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Mir;

public class Lowerer
{
    private readonly MutMirFunction _mirFun;
    private readonly FunDefinitionSymbol _funDefinition;
    
    private MutBasicBlock? _currentBlock;
    private readonly Dictionary<VariableSymbol, Place.Local> _variables = [];
    private readonly Dictionary<TypeSymbol, RefTypeSymbol> _refTypeSymbols = [];
    private readonly Stack<(MutBasicBlock Break, MutBasicBlock Continue)> _loopLabels = [];

    private TypeSymbol Unit => _funDefinition.Compilation.BaseLibrary.Unit;
    private TypeSymbol Never => _funDefinition.Compilation.BaseLibrary.Never;
    private TypeSymbol Error => _funDefinition.Compilation.BaseLibrary.Error;
    private TypeSymbol Bool => _funDefinition.Compilation.BaseLibrary.Bool;
    
    private Lowerer(FunDefinitionSymbol funDefinition)
    {
        _funDefinition = funDefinition;
        
        var returnPlace = new Place.Local(new LocalId(0), LocalKind.Return,
            funDefinition.IsReturnMut ? GetRefType(funDefinition.ReturnType) : funDefinition.ReturnType);
        var nextLocal = 1;
        var paramPlaces = ImmutableArray.CreateBuilder<Place.Local>();
        foreach (var param in funDefinition.SelfAndParameters)
        {
            var local = new Place.Local(new LocalId(nextLocal++),
                LocalKind.Parameter,
                param.IsMut
                    ? GetRefType(param.Type)
                    : param.Type,
                param.DeclarationSyntax
            );
            _variables[param] = local;
            paramPlaces.Add(local);
        }
        
        _mirFun = new MutMirFunction(funDefinition.FullName, returnPlace, paramPlaces.DrainToImmutable());
        _currentBlock = _mirFun.Entry;
    }

    public static MutMirFunction Lower(FunDefinitionSymbol fun, Compilation compilation)
    {
        if (fun.Body is not BoundBlock body)
            throw new ArgumentException("Cannot lower funs without blocks.", nameof(fun));

        var lowerer = new Lowerer(fun);
        lowerer.LowerStmt(body);
        
        if (lowerer._currentBlock is not null &&
            lowerer._currentBlock.Terminator is null)
        {
            if (fun.ReturnType == compilation.BaseLibrary.Unit)
            {
                lowerer.Emit(new Instruction.Assign(lowerer._mirFun.ReturnPlace, new Operand.Empty(compilation.BaseLibrary.Unit)));
                lowerer.Terminate(new Terminator.Return());
            }
            else
                lowerer.Terminate(new Terminator.MissingReturn());
        }

        return lowerer._mirFun;
    }


    private void Emit(Instruction instruction)
    {
        _currentBlock ??= _mirFun.AddBasicBlock();
        _currentBlock.Add(instruction);
    }

    private void Terminate(Terminator terminator)
    {
        _currentBlock ??= _mirFun.AddBasicBlock();
        _currentBlock.Terminator = terminator;
        _currentBlock = null;
    }

    private void Enter(MutBasicBlock block)
    {
        Debug.Assert(_currentBlock is null, "Current block must be terminated.");
        _currentBlock = block;
    }


    private RefTypeSymbol GetRefType(TypeSymbol type)
    {
        if (_refTypeSymbols.TryGetValue(type, out var refType))
            return refType;

        refType = RefTypeSymbol.Get(type);
        _refTypeSymbols[type] = refType;
        return refType;
    }

    private Operand.Ref MakeRef(Place place, SyntaxNode? syntax = null)
        => new(place, GetRefType(place.Type), syntax);
    
    
    public void LowerStmt(BoundStmt stmt)
    {
        switch (stmt)
        {
            case BoundValue value:
                LowerInto(value, new Place.Discard(Error));
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
        if (varDecl.Variable.Type == Never)
        {
            // Never does not create a variable that is written to.
            LowerInto(varDecl.Initializer, new Place.Discard(Error));
            return;
        }
        
        if (!_variables.TryGetValue(varDecl.Variable, out var local))
        {
            local = _mirFun.AddLocal(varDecl.Variable.Type, LocalKind.Variable, varDecl.Syntax);
            _variables[varDecl.Variable] = local;
        }
        
        LowerInto(varDecl.Initializer, local);
    }
    
    private void LowerWhile(BoundWhile @while)
    {
        var conditionBlock = _mirFun.AddBasicBlock("while condition");
        var blockBlock = _mirFun.AddBasicBlock("while body");
        var joinBlock = _mirFun.AddBasicBlock("while join");
        
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
        var trueBlock = _mirFun.AddBasicBlock("if true");
        var joinBlock = _mirFun.AddBasicBlock("if join");
        var falseBlock = ifStmt.Else is not null ? _mirFun.AddBasicBlock("if false") : joinBlock;

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
                
                // Anything that came in as a ref needs to be dereferenced.
                if (place.Type is RefTypeSymbol)
                    place = new Place.Deref(place);
                return place;
            }
            case BoundField field:
                return new Place.Field(LowerPlace(field.Receiver), field.Field, field.Syntax);
            case BoundCall { IsMutReturn: true } call:
                // Lower the call target into a temp with ref type
                // and hand over the deref.
                var refTemp = _mirFun.AddTemp(GetRefType(call.Type));
                LowerCallInstruction(call, refTemp);

                return new Place.Deref(refTemp);
            
            case BoundError:
                return new Place.Error(Error);
                
            default:
                var temp = _mirFun.AddTemp(value.Type);
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

        BoundIfExpr boundIfExpr => LowerIfExpr(boundIfExpr, _mirFun.AddTemp(boundIfExpr.Type)),

        BoundLogicAnd boundLogicAnd => LowerLogicOperator(boundLogicAnd, _mirFun.AddTemp(boundLogicAnd.Type)),
        BoundLogicOr boundLogicOr => LowerLogicOperator(boundLogicOr, _mirFun.AddTemp(boundLogicOr.Type)),
        BoundAssign boundAssign => LowerAssign(boundAssign),

        BoundBreak => LowerBreak(),
        BoundReturn boundReturn => LowerReturn(boundReturn),
        BoundContinue => LowerContinue(),

        BoundCall boundCall => LowerCallOperand(boundCall, _mirFun.AddTemp(boundCall.Type)),
        BoundStructInit boundStructInit => LowerStructInit(boundStructInit, _mirFun.AddTemp(boundStructInit.Type)),

        BoundError => new Operand.Error(Error),
        
        BoundFieldInit or BoundValueOrTypeRef => throw new UnreachableException(),
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
                return LowerCallOperand(call, target);
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
        // If the target is a call returning a mut value,
        // it may have side effects. These must be evaluated
        // after the arguments because of
        //    `buffer.At(0) = buffer.ReallocateAndMutReturn(0)`
        // The reference must be directly used.
        
        if (assign.Target is BoundCall)
        {
            var valueTemp = _mirFun.AddTemp(assign.Value.Type);
            LowerInto(assign.Value, valueTemp);
            var place = LowerPlace(assign.Target);
            Emit(new Instruction.Assign(place, new Operand.Copy(valueTemp), assign.Syntax));
        }
        else
            LowerInto(assign.Value, LowerPlace(assign.Target));
        return new Operand.Empty(Unit);
    }


    private Operand LowerCallOperand(BoundCall call, Place target)
    {
        if (call.IsMutReturn)
        {
            // Call returns a reference. We need to store the ref
            // into a temp and deref it into target, since we are
            // in value position.
            var refTarget = _mirFun.AddTemp(GetRefType(call.Type));
            LowerCallInstruction(call, refTarget);
            Emit(new Instruction.Assign(target, new Operand.Copy(new Place.Deref(refTarget))));
        }
        else
            LowerCallInstruction(call, target);

        return new Operand.Copy(target);
    }

    private void LowerCallInstruction(BoundCall call, Place target)
    {
        var arguments = LowerArguments(call.ReceiverAndArguments, call.Fun.SelfAndParameters);

        if (call.Fun.OriginalDefinition.Body is Intrinsic intrinsic)
        {
            if (OperatorKind.TryFrom(intrinsic) is { } opKind)
                Emit(new Instruction.Operator(opKind, target, arguments, call.Syntax));
            else if (VmCallKind.TryFrom(intrinsic) is { } vmCallKind)
                Emit(new Instruction.VmCall(vmCallKind, target, arguments, call.Syntax));
            else
            {
                throw new UnreachableException($"Unknown intrinsic {intrinsic}");
            }
        }
        else
            Emit(new Instruction.Call(call.Fun, target, arguments, call.Syntax));

        if (call.Type == Never)
            Terminate(new Terminator.Unreachable());
    }

    private ImmutableArray<Operand> LowerArguments(ImmutableArray<BoundArgument> boundArguments,
        ImmutableArray<ParameterSymbol> parameters)
    {
        Debug.Assert(boundArguments.Length == parameters.Length);

        // If any argument might have a side effect, we need to lower
        // every argument into a temp to respect evaluation order.
        // Without side effects, this is not necessary.

        var mightHaveSideEffect = boundArguments.Any(arg => MightHaveSideEffects(arg.Value));
        var arguments = ImmutableArray.CreateBuilder<Operand>(boundArguments.Length);
        for (var i = 0; i < boundArguments.Length; i++)
        {
            if (parameters[i].IsMut)
            {
                Debug.Assert(boundArguments[i].IsMut);
                arguments.Add(MakeRef(LowerPlace(boundArguments[i].Value)));
            }
            else if (boundArguments[i].Value is BoundConst || !mightHaveSideEffect)
            {
                Debug.Assert(!boundArguments[i].IsMut);
                arguments.Add(LowerOperand(boundArguments[i].Value));
            }
            else
            {
                Debug.Assert(!boundArguments[i].IsMut);
                arguments.Add(LowerInto(boundArguments[i].Value, _mirFun.AddTemp(boundArguments[i].Type)));
            }
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
        var trueBlock = _mirFun.AddBasicBlock("if true");
        var falseBlock = _mirFun.AddBasicBlock("if false");
        var joinBlock = _mirFun.AddBasicBlock("if join");
        
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

        var assignLeftBlock = _mirFun.AddBasicBlock();
        var assignRightBlock = _mirFun.AddBasicBlock();
        var joinBlock = _mirFun.AddBasicBlock();

        var left = LowerInto(boundLeft, _mirFun.AddTemp(boundLeft.Type));
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
        return new Operand.Unreachable(Never);
    }

    private Operand LowerContinue()
    {
        Debug.Assert(_loopLabels.Count > 0);
        Terminate(new Terminator.Goto(_loopLabels.Peek().Continue));
        return new Operand.Unreachable(Never);
    }

    private Operand LowerReturn(BoundReturn @return)
    {
        if (@return.Value is not null)
        {
            if (@return.IsMut)
            {
                var place = LowerPlace(@return.Value);
                Emit(new Instruction.Assign(_mirFun.ReturnPlace, MakeRef(place)));
            }
            else
                LowerInto(@return.Value, _mirFun.ReturnPlace);
        }
        else
            Emit(new Instruction.Assign(_mirFun.ReturnPlace, new Operand.Empty(Unit)));

        Terminate(new Terminator.Return(@return.Syntax));
        return new Operand.Unreachable(Never);
    }
}