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

    public FunBody Body
    {
        get
        {
            Guard.IsState(_body is not null, "Body has not been bound yet.");
            return _body.Value;
        }

        // Body is only set during construction (intrinsics, generated) or
        // during binding. So the binder needs to ensure, that it is set.
        internal set
        {
            Guard.IsState(_body is null, "Cannot set body twice.");
            _body = value;
        }
    }
    

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
            Body = body.Value;
        
        // Set parameter owners
        foreach (var parameter in Parameters)
            parameter.Owner = this;
    }

    
}