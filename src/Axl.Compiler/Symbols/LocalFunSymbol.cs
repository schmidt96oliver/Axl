using System.Collections.Immutable;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class LocalFunSymbol(FunDeclSyntax declarationSyntax, Symbol? owner)
    : FunSymbol(declarationSyntax.Name.Text.ToString(), owner, isPublic: false)
{
    public override FunDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;
    
    private ImmutableArray<ParameterSymbol>? _parameters;

    public override ImmutableArray<ParameterSymbol> Parameters => _parameters ?? throw new InvalidOperationException(
        $"Parameters of '{Name}' not bound yet.");

    private ParameterSymbol? _selfParameter;

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter => _selfParameter;

    private TypeSymbol? _returnType;
    private bool? _isReturnMut;

    public override TypeSymbol ReturnType => _returnType ??
                                             throw new InvalidOperationException(
                                                 $"{nameof(ReturnType)} of '{Name}' not bound yet.");

    /// <inheritdoc />
    public override bool IsReturnMut => _isReturnMut ?? throw new InvalidOperationException(
        $"{nameof(IsReturnMut)} of '{Name}' not bound yet.");

    private FunBody? _body;

    public override FunBody Body =>
        _body ?? throw new InvalidOperationException($"{nameof(Body)} of '{Name}' not bound yet.");

    internal void SetSignature(ParameterSymbol? selfParameter, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, bool isReturnMut)
    {
        Guard.IsState(_parameters is null && _returnType is null, $"Signature of '{Name}' already bound.");
        _parameters = parameters;
        _returnType = returnType;
        _selfParameter = selfParameter;
        _isReturnMut = isReturnMut;
    }
    
    internal void SetBody(BoundBlock body)
    {
        Guard.IsState(_body is null, $"{nameof(Body)} of '{Name}' already bound.");
        _body = body;
    }
}