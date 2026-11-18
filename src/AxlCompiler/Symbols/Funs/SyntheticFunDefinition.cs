using System.Collections.Immutable;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class SyntheticFunDefinition(Compilation compilation, string name, Symbol? owner, bool isPublic)
    : FunDefinition(compilation, name, owner, isPublic)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => SyntaxFacts.IsIdentifier(Name);

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [];

    /// <inheritdoc />
    public override FunDeclSyntax? DeclarationSyntax => null;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [];

    private ImmutableArray<ParameterSymbol> _parameters;
    private ParameterSymbol? _selfParameter;
    private TypeSymbol? _returnType;
    private FunBody? _body;

    /// <inheritdoc />
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (_parameters.IsDefault)
                throw new InvalidOperationException($"{nameof(Parameters)} not bound yet.");
            return _parameters;
        }
    }

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter
    {
        get
        {
            // Self is always bound together with parameters.
            if (_parameters.IsDefault)
                throw new InvalidOperationException($"{nameof(Parameters)} not bound yet.");
            return _selfParameter;
        }
    }

    /// <inheritdoc />
    public override TypeSymbol ReturnType
        => _returnType ?? throw new InvalidOperationException($"{nameof(ReturnType)} not bound yet.");

    /// <inheritdoc />
    public override bool IsReturnMut => false;

    /// <inheritdoc />
    public override FunBody Body =>
        _body ?? throw new InvalidOperationException($"{nameof(Body)} not bound yet.");

    public void SetSignature(ParameterSymbol? selfParameter,
        ImmutableArray<ParameterSymbol> parameters,
        TypeSymbol returnType)
    {
        Guard.MustBe(_parameters.IsDefault, "Signature already bound.");
        Guard.MustBe(selfParameter is null || selfParameter.Owner == Identity);
        Guard.MustBe(parameters.All(p => p.Owner == Identity));

        _selfParameter = selfParameter;
        _parameters = parameters;
        _returnType = returnType;
    }

    public void SetBody(FunBody body)
    {
        Guard.MustBe(_body is null, "Body already bound.");
        _body = body;
    }
}