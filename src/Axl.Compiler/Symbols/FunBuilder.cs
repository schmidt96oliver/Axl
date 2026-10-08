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
public sealed class FunBuilder
{
    private readonly List<ParameterSymbol> _parameters = [];
    private FunSymbol _fun;
    private readonly BaseNamespaceSymbol _baseNamespace;
    private BoundStmt? _last = null;
    private bool _isFinished = false;


    private FunBuilder(FunSymbol fun, BaseNamespaceSymbol baseNamespace)
    {
        _fun = fun;
        _baseNamespace = baseNamespace;
    }
    

    public static FunBuilder Method(string name, Symbol parent, TypeSymbol receiverType, BaseNamespaceSymbol baseNamespace,
        TypeSymbol? returnType = null, bool isPublic = true)
    {
        var fun = new FunSymbol(name, parent, isPublic, receiverType, returnType ?? baseNamespace.Unit);
        return new FunBuilder(fun, baseNamespace);
    }

    public static FunBuilder Method(TokenKind tokenName, Symbol parent, TypeSymbol receiverType,
        BaseNamespaceSymbol baseNamespace, TypeSymbol? returnType = null, bool isPublic = true)
        => Method(name: SyntaxFacts.GetText(tokenName) ??
                        throw new ArgumentException($"{nameof(tokenName)} has no token text.", nameof(tokenName)),
            parent, receiverType, baseNamespace, returnType, isPublic);

    public static FunBuilder Static(string name, Symbol parent,
        BaseNamespaceSymbol baseNamespace, TypeSymbol? returnType = null, bool isPublic = true)
    {
        var fun = new FunSymbol(name, parent, isPublic, null, returnType ?? baseNamespace.Unit);
        return new FunBuilder(fun, baseNamespace);
    }

    public BoundSelf Self
    {
        get
        {
            Guard.IsState(_fun.ReceiverType is not null);
            return new BoundSelf(_fun.ReceiverType);
        }
    }

    public BoundVariable Arg0
        => new(_parameters[0]);

    public void Param(string name, TypeSymbol type)
    {
        var paramSymbol = new ParameterSymbol(name, _fun, type);
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
        Guard.MustBe(fun.ReceiverType is null);
        Guard.MustBe(arguments.Select(arg => arg.Type).SequenceEqual(fun.ParameterTypes));

        return Set(new BoundCall(fun, null, [.. arguments]));
    }
    
    public BoundReturn Return(BoundValue? expr = null)
    {
        var exprType = expr?.Type ?? _baseNamespace.Unit;
        Guard.MustBe(_fun.ReturnType == exprType);
        return Set(new BoundReturn(expr));
    }

    public BoundBlock Block(params ImmutableArray<BoundStmt> stmts)
    {
        return Set(new BoundBlock(stmts, [], [], _baseNamespace.Unit));
    }

    private T Set<T>(T expr)
        where T : BoundStmt
    {
        _last = expr;
        return expr;
    }

    public FunSymbol ToFun()
    {
        Guard.IsState(!_isFinished);

        var body = GetBody();
        _fun.SetParameters([.. _parameters]);
        _fun.SetBody(body);
        return _fun;
    }

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
            if (_fun.ReturnType == _baseNamespace.Unit)
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
        Guard.MustBe(left.Type == _baseNamespace.Bool && right.Type == _baseNamespace.Bool);
        return Set(new BoundOr(left, right, _baseNamespace.Bool));
    }


    public union StringPart(BoundValue, string);
    
    public BoundStringExpr String(params ImmutableArray<StringPart> parts)
    {
        Guard.IsState(parts.All(part => part is not BoundValue expr || expr.Type == _baseNamespace.String));
        return Set(new BoundStringExpr([
            .. parts.Select(part => part switch
            {
                BoundValue expr => expr,
                string str => new BoundConst(str, _baseNamespace.String)
            })
        ], _baseNamespace.String));
    }

    public BoundValue True()
        => Set(new BoundConst(true, _baseNamespace.Bool));

}