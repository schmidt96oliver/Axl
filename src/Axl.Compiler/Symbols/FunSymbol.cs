using System.Collections.Immutable;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public sealed class FunSymbol : Symbol
{
    /// <summary>
    /// The <see cref="TypeSymbol"/> this fun gets as an implicit
    /// 'self' argument. If this fun is a method, <see cref="ReceiverType"/>
    /// denotes the type it is a method of. <c>null</c>, if this
    /// method is free or a static member.
    /// </summary>
    public TypeSymbol? ReceiverType { get; }
    
    public ImmutableArray<ParameterSymbol> Parameters { get; }
    
    public ImmutableArray<TypeSymbol> ParameterTypes { get; }
    
    public TypeSymbol ReturnType { get; }
    public FunDeclSyntax? DeclarationSyntax { get; }

    private FunBody? _body;

    public FunBody Body => _body ?? throw new InvalidOperationException($"Body of '{Name}' not bound yet.");
    

    public FunSymbol(string name,
        TypeSymbol? receiverType,
        ImmutableArray<ParameterSymbol> parameters,
        TypeSymbol returnType,
        FunBody? body = null,
        FunDeclSyntax? declarationSyntax = null) : base(name)
    {
        ReceiverType = receiverType;
        Parameters = parameters;
        ParameterTypes = [.. parameters.Select(parameter => parameter.Type)];
        ReturnType = returnType;
        DeclarationSyntax = declarationSyntax;

        if (body is not null)
            SetBody(body.Value);
        
        // Set parameter owners
        foreach (var parameter in Parameters)
            parameter.Owner = this;
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
}