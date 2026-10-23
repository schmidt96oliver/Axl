using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class FieldSymbol(
    FieldDeclSyntax declarationSyntax,
    Scope enclosingScope,
    StructSymbol owner,
    bool isPublic)
    : Symbol(declarationSyntax.Name.Text.ToString(), owner)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _binder = null;

    private bool _isBindingType = false;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;
    
    public FieldDeclSyntax DeclarationSyntax { get; } = declarationSyntax;
    public override bool IsPublic { get; } = isPublic;

    public TypeSymbol Type
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
        _binder!.SideEffects.DrainInto(sideEffects);
        _binder = null;
    }
}