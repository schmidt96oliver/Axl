using System.Collections.Immutable;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class FieldDefinitionSymbol(
    FieldDeclSyntax declarationSyntax,
    Scope enclosingScope,
    TypeSymbol owner)
    : FieldSymbol(declarationSyntax.Name.Text.ToString(),
        owner, isPublic: declarationSyntax.PubKw is not null)
{
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingType = false;
    private readonly List<SpecializedFieldSymbol> _specializations = [];

    public FieldDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [DeclarationSyntax];
    
    public override FieldDefinitionSymbol OriginalDefinition => this;
    
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
    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = Type;
        sideEffects.AddFrom(_sideEffects);
    }


    public FieldSymbol Specialize(TypeSymbol specializedOwner)
    {
        Guard.MustBe(Equals(specializedOwner.OriginalDefinition, Owner?.OriginalDefinition));

        // If nothing changes, just return this definition.
        if (Equals(specializedOwner, Owner))
            return this;

        var specialization = _specializations.FirstOrDefault(s => Equals(s.Owner, specializedOwner));
        if (specialization is null)
        {
            specialization = new SpecializedFieldSymbol(originalDefinition: this, specializedOwner);
            _specializations.Add(specialization);
        }

        return specialization;
    }
}