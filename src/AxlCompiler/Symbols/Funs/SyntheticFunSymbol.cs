using System.Collections.Immutable;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public class SyntheticFunSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic)
    : FunDefinitionSymbol(compilation, name, owner, isPublic)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => SyntaxFacts.IsIdentifier(Name);

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [];

    /// <inheritdoc />
    public override FunDeclSyntax? DeclarationSyntax => null;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [];

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    /// <inheritdoc />
    public override ImmutableArray<ParameterSymbol> Parameters => [];

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter => null;


    private TypeSymbol? _returnType;
    private FunBody? _body;
    
    /// <inheritdoc />
    public override TypeSymbol ReturnType 
        => _returnType ?? throw new InvalidOperationException($"{nameof(ReturnType)} not bound yet.");

    /// <inheritdoc />
    public override bool IsReturnMut => false;

    /// <inheritdoc />
    public override FunBody Body =>
        _body ?? throw new InvalidOperationException($"{nameof(Body)} not bound yet.");


    public void SetReturnValue(BoundValue value)
    {
        Guard.MustBe(_returnType is null && _body is null);
        _returnType = value.Type;
        _body = new BoundBlock([new BoundReturn(value, isMut: false, Compilation.BaseLibrary.Never)],
            Compilation.BaseLibrary.Unit);
    }
}