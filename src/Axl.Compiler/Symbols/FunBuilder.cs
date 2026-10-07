using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

/// <summary>
/// Provides ergonomic methods to generate a function with body.
/// </summary>
public sealed class FunBuilder(BaseNamespaceSymbol baseNamespace)
{
    private readonly List<ParameterSymbol> _parameters = [];
    private TypeSymbol? _receiverType = null;
    private TypeSymbol? _returnType = null;
    private BoundStmt? _last = null;
    private bool _isFinished = false;

    public BoundSelf Self
    {
        get
        {
            Guard.IsState(_receiverType is not null);
            return new BoundSelf(_receiverType);
        }
    }

    public BoundVariable Arg0
        => new(_parameters[0]);

    public void Receiver(TypeSymbol type)
    {
        Guard.IsState(_receiverType is null);
        _receiverType = type;
    }

    public void Param(string name, TypeSymbol type)
    {
        var paramSymbol = new ParameterSymbol(name, type);
        _parameters.Add(paramSymbol);
    }

    public BoundCall InstanceCall(FunSymbol fun, BoundValue receiver, params ImmutableArray<BoundValue> arguments)
    {
        Guard.MustBe(receiver.Type == fun.ReceiverType);
        Guard.MustBe(arguments.Select(arg => arg.Type).SequenceEqual(fun.ParameterTypes));

        return Set(new BoundCall(fun, receiver, [.. arguments]));
    }

    public BoundCall StaticCall(FunSymbol fun, params ImmutableArray<BoundValue> arguments)
    {
        Guard.IsState(_receiverType is null);
        Guard.MustBe(arguments.Select(arg => arg.Type).SequenceEqual(fun.ParameterTypes));

        return Set(new BoundCall(fun, null, [.. arguments]));
    }
    
    public BoundReturn Return(BoundValue? expr = null)
    {
        var exprType = expr?.Type ?? baseNamespace.Unit;
        _returnType ??= exprType;

        Guard.MustBe(exprType == _returnType);
        return Set(new BoundReturn(expr));
    }

    public BoundBlock Block(params ImmutableArray<BoundStmt> stmts)
    {
        return Set(new BoundBlock(stmts, [], baseNamespace.Unit));
    }

    private T Set<T>(T expr)
        where T : BoundStmt
    {
        _last = expr;
        return expr;
    }

    public FunSymbol ToFun(string name)
    {
        Guard.IsState(!_isFinished);

        var body = GetBody();
        if (_returnType is null)
            throw new InvalidOperationException("Missing return."); 
        
        return new FunSymbol(name, _receiverType, [.. _parameters], _returnType, body);
    }
    
    public FunSymbol ToFun(TokenKind tokenName)
        => ToFun(SyntaxFacts.GetText(tokenName) ?? throw new ArgumentException($"{nameof(tokenName)} has no token text.", nameof(tokenName)));

    private BoundBlock GetBody()
    {
        if (_last is null)
            Return();
        Debug.Assert(_last is not null);

        if (_last is not BoundBlock)
            Block(_last);
        Debug.Assert(_last is BoundBlock);

        if (!_last.IsDiverging)
        {
            if (_returnType is null || _returnType == baseNamespace.Unit)
            {
                var block = (BoundBlock)_last;
                Block([.. block.Stmts, Return()]);
            }
            else
                throw new InvalidOperationException("Missing return.");
        }

        _isFinished = true;
        return (BoundBlock)_last;
    }

    public BoundValue Or(BoundValue left, BoundValue right)
    {
        Guard.MustBe(left.Type == baseNamespace.Bool && right.Type == baseNamespace.Bool);
        return Set(new BoundOr(left, right, baseNamespace.Bool));
    }


    public union StringPart(BoundValue, string);
    
    public BoundStringExpr String(params ImmutableArray<StringPart> parts)
    {
        Guard.IsState(parts.All(part => part is not BoundValue expr || expr.Type == baseNamespace.String));
        return Set(new BoundStringExpr([
            .. parts.Select(part => part switch
            {
                BoundValue expr => expr,
                string str => new BoundConst(str, baseNamespace.String)
            })
        ], baseNamespace.String));
    }

    public BoundValue True()
        => Set(new BoundConst(true, baseNamespace.Bool));

}