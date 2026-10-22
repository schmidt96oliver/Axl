using System.Collections.Immutable;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class SyntheticFunSymbol(
    string name,
    Symbol owner,
    bool isPublic,
    TypeSymbol? receiverType,
    bool isMutatingReceiver)
    : FunSymbol(name, owner, isPublic, receiverType, isMutatingReceiver)
{
    /// <inheritdoc />
    public override FunDeclSyntax? DeclarationSyntax => null;
    
    private ImmutableArray<ParameterSymbol>? _parameters;
    public override ImmutableArray<ParameterSymbol> Parameters => _parameters ?? throw new InvalidOperationException(
        $"Parameters of '{Name}' not bound yet.");

    private TypeSymbol? _returnType;
    public override TypeSymbol ReturnType => _returnType ?? throw new InvalidOperationException($"{nameof(ReturnType)} of '{Name}' not bound yet.");

    private FunBody? _body;

    public override FunBody Body => _body ?? throw new InvalidOperationException($"{nameof(Body)} of '{Name}' not bound yet.");

    internal void SetSignatureAndBody(ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, FunBody body)
    {
        SetSignature(parameters, returnType);
        SetBody(body);
    }

    internal void SetSignature(ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
    {
        Guard.IsState(_parameters is null && _returnType is null,
            $"{nameof(Parameters)} or {nameof(ReturnType)} of '{Name}' already bound.");
        _parameters = parameters;
        _returnType = returnType;
    }

    internal void SetBody(FunBody body)
    {
        Guard.IsState(_body is null, $"{nameof(Body)} of '{Name}' already bound.");
        _body = body;
    }
}