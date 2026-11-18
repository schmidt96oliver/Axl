using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class FieldDefinition(
    FieldDeclSyntax declarationSyntax,
    Scope enclosingScope,
    TypeSymbol owner)
    : Definition<FieldSymbol>(owner.Compilation, declarationSyntax.Name.Text.ToString(),
        owner, isPublic: declarationSyntax.PubKw is not null)
{
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingType = false;

    public FieldDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

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

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = Type;
        sideEffects.MergeWith(_sideEffects);
    }

    protected override FieldSymbol Create(Symbol? owner, ImmutableArray<TypeSymbol>? typeArguments)
        => new(this, (TypeSymbol)owner!, isIdentity: typeArguments is null);
}