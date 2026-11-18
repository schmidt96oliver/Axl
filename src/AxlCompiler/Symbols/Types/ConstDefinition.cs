using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class ConstDefinition(
    ConstDeclSyntax declarationSyntax,
    Scope enclosingScope,
    Symbol owner)
    : Definition<ConstSymbol>(owner.Compilation, declarationSyntax.Name.Text.ToString(),
        owner, isPublic: declarationSyntax.PubKw is not null)
{
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingType = false;
    private bool _isBindingValueFun = false;

    public ConstDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [DeclarationSyntax];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [DeclarationSyntax.Name];

    public TypeSymbol Type
    {
        get
        {
            if (field is null)
            {
                if (_isBindingType)
                    throw new CycleException();
                _isBindingType = true;

                field = new Binder(enclosingScope.WithOwner(Identity), _sideEffects)
                    .BindType(DeclarationSyntax.TypeExpr, TypeUsage.Signature).Type;

                _isBindingType = false;
            }

            return field;
        }
    }

    public SyntheticFunDefinition ValueFun
    {
        get
        {
            if (field is null)
            {
                if (_isBindingValueFun)
                    throw new CycleException();
                _isBindingValueFun = true;

                var boundValue = new Binder(enclosingScope.WithOwner(Identity), _sideEffects)
                    .BindConstInitializer(DeclarationSyntax.ValueExpr);
                field = new SyntheticFunDefinition(Compilation, name: $"<const init>{Name}", Owner, isPublic: false);
                field.SetSignature(selfParameter: null, parameters: [], returnType: Type);
                field.SetBody(new BoundBlock(
                    [
                        new BoundReturn(boundValue, isMut: false, Compilation.BaseLibrary.Never)
                    ],
                    Compilation.BaseLibrary.Unit));

                _isBindingValueFun = false;
            }

            return field;
        }
    }

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = Type;
        _ = ValueFun;
        sideEffects.MergeWith(_sideEffects);
    }

    protected override ConstSymbol Create(Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments)
        => new(this, owner!, isIdentity: typeArguments is null);
}