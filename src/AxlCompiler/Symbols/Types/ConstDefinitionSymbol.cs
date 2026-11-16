using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class ConstDefinitionSymbol(
    ConstDeclSyntax declarationSyntax,
    Scope enclosingScope,
    Symbol owner)
    : ConstSymbol(declarationSyntax.Name.Text.ToString(),
        owner, isPublic: declarationSyntax.PubKw is not null)
{
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingType = false;
    private bool _isBindingValueFun = false;
    private readonly List<SpecializedConstSymbol> _specializations = [];

    public ConstDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [DeclarationSyntax];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [DeclarationSyntax.Name];

    public override ConstDefinitionSymbol OriginalDefinition => this;
    
    public override TypeSymbol Type
    {
        get
        {
            if (field is null)
            {
                if (_isBindingType)
                    throw new CycleException();
                _isBindingType = true;

                field = new Binder(enclosingScope.WithOwner(this), _sideEffects)
                    .BindType(DeclarationSyntax.TypeExpr, TypeUsage.Signature).Type;

                _isBindingType = false;
            }

            return field;
        }
    }

    /// <inheritdoc />
    public override SyntheticFunSymbol ValueFun
    {
        get
        {
            if (field is null)
            {
                if (_isBindingValueFun)
                    throw new CycleException();
                _isBindingValueFun = true;

                var boundValue = new Binder(enclosingScope.WithOwner(this), _sideEffects)
                    .BindConstInitializer(DeclarationSyntax.ValueExpr);
                field = new SyntheticFunSymbol(Compilation, name: $"<const init>{Name}", Owner, isPublic: false);
                field.SetReturnValue(boundValue);
                
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


    public ConstSymbol Specialize(Symbol specializedOwner)
    {
        Guard.MustBe(Equals(specializedOwner.OriginalDefinition, Owner?.OriginalDefinition));

        // If nothing changes, just return this definition.
        if (Equals(specializedOwner, Owner))
            return this;

        var specialization = _specializations.FirstOrDefault(s => Equals(s.Owner, specializedOwner));
        if (specialization is null)
        {
            specialization = new SpecializedConstSymbol(originalDefinition: this, specializedOwner);
            _specializations.Add(specialization);
        }

        return specialization;
    }
}