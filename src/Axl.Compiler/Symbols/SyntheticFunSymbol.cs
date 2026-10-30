using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class SyntheticFunSymbol(string name, Symbol owner, bool isPublic)
    : FunSymbol(owner.Compilation, name, owner, isPublic)
{
    /// <inheritdoc />
    public override FunDeclSyntax? DeclarationSyntax => null;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => Name is not "";

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    private ImmutableArray<ParameterSymbol>? _parameters;
    public override ImmutableArray<ParameterSymbol> Parameters => _parameters ?? throw new InvalidOperationException(
        $"Parameters of '{Name}' not bound yet.");

    private ParameterSymbol? _selfParameter;

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter => _selfParameter;

    private TypeSymbol? _returnType;
    private bool? _isReturnMut;
    public override TypeSymbol ReturnType => _returnType ?? throw new InvalidOperationException($"{nameof(ReturnType)} of '{Name}' not bound yet.");
    public override bool IsReturnMut => _isReturnMut ?? throw new InvalidOperationException($"{nameof(IsReturnMut)} of '{Name}' not bound yet.");

    private FunBody? _body;

    public override FunBody Body => _body ?? throw new InvalidOperationException($"{nameof(Body)} of '{Name}' not bound yet.");

    internal void SetSignature(ParameterSymbol? selfParameter, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, bool isReturnMut = false)
    {
        Guard.IsState(_parameters is null && _returnType is null, $"Signature of '{Name}' already bound.");
        _parameters = parameters;
        _returnType = returnType;
        _selfParameter = selfParameter;
        _isReturnMut = isReturnMut;
    }

    internal void SetBody(FunBody body)
    {
        Guard.IsState(_body is null, $"{nameof(Body)} of '{Name}' already bound.");
        _body = body;
    }
}