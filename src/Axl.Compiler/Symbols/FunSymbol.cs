using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class FunSymbol(
    string name,
    TypeSymbol? receiverType,
    ImmutableArray<ParameterSymbol> parameters,
    TypeSymbol returnType,
    Intrinsic? intrinsic = null,
    FunDeclSyntax? declarationSyntax = null)
    : Symbol(name)
{
    public override SymbolKind Kind => SymbolKind.Fun;

    /// <summary>
    /// The <see cref="TypeSymbol"/> this fun gets as an implicit
    /// 'self' argument. If this fun is a method, <see cref="ReceiverType"/>
    /// denotes the type it is a method of. <c>null</c>, if this
    /// method is free or a static member.
    /// </summary>
    public TypeSymbol? ReceiverType { get; } = receiverType;
    
    public ImmutableArray<ParameterSymbol> Parameters { get; } = parameters;
    
    public ImmutableArray<TypeSymbol> ParameterTypes { get; } = [.. parameters.Select(parameter => parameter.Type)];
    
    public TypeSymbol ReturnType { get; } = returnType;
    public FunDeclSyntax? DeclarationSyntax { get; } = declarationSyntax;

    public Intrinsic? Intrinsic { get; } = intrinsic;
}