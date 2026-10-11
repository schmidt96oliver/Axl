using System.Collections.Immutable;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public sealed class FunSymbol : Symbol
{
    public override bool IsPublic { get; }

    /// <summary>
    /// The <see cref="TypeSymbol"/> this fun gets as an implicit
    /// 'self' argument. If this fun is a method, <see cref="ReceiverType"/>
    /// denotes the type it is a method of. <c>null</c>, if this
    /// method is free or a static member.
    /// </summary>
    public TypeSymbol? ReceiverType { get; }

    /// <summary>
    /// Whether this fun is allowed to mutate its receiver.
    /// </summary>
    public bool IsMutatingReceiver { get; }
    
    private ImmutableArray<ParameterSymbol>? _parameters;
    public ImmutableArray<ParameterSymbol> Parameters => _parameters ?? throw new InvalidOperationException(
        $"Parameters of '{Name}' not bound yet.");


    public ImmutableArray<TypeSymbol> ParameterTypes => [.. Parameters.Select(parameter => parameter.Type)];

    private TypeSymbol? _returnType;
    public TypeSymbol ReturnType => _returnType ?? throw new InvalidOperationException($"{nameof(ReturnType)} of '{Name}' not bound yet.");
    public FunDeclSyntax? DeclarationSyntax { get; }

    private FunBody? _body;

    public FunBody Body => _body ?? throw new InvalidOperationException($"{nameof(Body)} of '{Name}' not bound yet.");
    public bool IsStatic => ReceiverType is null;


    public FunSymbol(string name,
        Symbol owner,
        bool isPublic,
        TypeSymbol? receiverType,
        bool isMutatingReceiver,
        FunDeclSyntax? declarationSyntax = null) : base(name, owner)
    {
        Guard.MustBe(!isMutatingReceiver || receiverType is not null);
        
        IsPublic = isPublic;
        ReceiverType = receiverType;
        IsMutatingReceiver = isMutatingReceiver;
        DeclarationSyntax = declarationSyntax;
    }

    /// <summary>
    /// Bodies must be bound after declaring all funs, so this must be two phases.
    /// Must be set exactly once during binding.
    /// </summary>
    internal void SetBody(FunBody body)
    {
        Guard.IsState(_body is null, $"Body of '{Name}' already bound.");
        _body = body;
    }

    internal void SetParametersAndReturnType(ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
    {
        Guard.IsState(_parameters is null && _returnType is null, $"{nameof(Parameters)} or {nameof(ReturnType)} of '{Name}' already bound.");
        _parameters = parameters;
        _returnType = returnType;
    }
}