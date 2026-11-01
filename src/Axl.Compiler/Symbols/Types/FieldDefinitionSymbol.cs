using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class FieldDefinitionSymbol(
    FieldDeclSyntax declarationSyntax,
    Scope enclosingScope,
    TypeSymbol owner)
    : FieldSymbol(declarationSyntax.Name.Text.ToString(),
        owner, isPublic: declarationSyntax.PubKw is not null)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _binder = null;
    private bool _isBindingType = false;
    private readonly List<SpecializedFieldSymbol> _specializations = [];

    public FieldDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

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
                
                _binder ??= MakeBinder();
                field = _binder.BindType(DeclarationSyntax.TypeExpr, TypeUsage.Signature);

                _isBindingType = false;
            }

            return field;
        }
    }

    private Binder MakeBinder()
        => new(enclosingScope.WithOwner(this), new BinderSideEffects());

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = Type;
        _binder?.SideEffects.DrainInto(sideEffects);
        _binder = null;
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