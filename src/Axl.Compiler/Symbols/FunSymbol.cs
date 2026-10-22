using System.Collections.Immutable;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public union FunBody(Intrinsic, BoundBlock);

public closed class FunSymbol : Symbol
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

    public bool IsOperator => SyntaxFacts.IsOperator(Name);

    public bool IsStatic => ReceiverType is null;

    public abstract FunDeclSyntax? DeclarationSyntax { get; }


    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

    public ImmutableArray<TypeSymbol> ParameterTypes => [.. Parameters.Select(parameter => parameter.Type)];

    public abstract TypeSymbol ReturnType { get; }

    public abstract FunBody Body { get; }


    protected FunSymbol(string name,
        Symbol? owner,
        bool isPublic,
        TypeSymbol? receiverType,
        bool isMutatingReceiver) : base(name, owner)
    {
        Guard.MustBe(!isMutatingReceiver || receiverType is not null);

        IsPublic = isPublic;
        ReceiverType = receiverType;
        IsMutatingReceiver = isMutatingReceiver;
    }
}