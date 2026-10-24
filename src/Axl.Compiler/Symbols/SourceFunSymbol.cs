using System.Collections.Immutable;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class SourceFunSymbol(
    FunDeclSyntax declarationSyntax,
    Scope enclosingScope,
    bool isPublic,
    BoundAnnotation? intrinsicAnnotation)
    : FunSymbol(declarationSyntax.Name.Text.ToString(), owner: enclosingScope.Owner, isPublic)
{
    public override FunDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _signatureBinder = null;
    private bool _isBindingParameters = false;
    private bool _isBindingReturnType = false;
    private bool _isBindingBody = false;
    private bool _isSelfParameterBound = false;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;
    
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (field.IsDefault)
            {
                if (_isBindingParameters)
                    throw new CycleException();
                _isBindingParameters = true;
                
                _signatureBinder ??= MakeSignatureBinder();
                field = _signatureBinder.BindParameters(DeclarationSyntax.ParameterList);

                _isBindingParameters = false;

                if (IsOperator)
                    _signatureBinder.CheckOperatorSignature(this);
            }

            return field;
        }
    }

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter
    {
        get
        {
            if (!_isSelfParameterBound)
            {
                var type = DeclarationSyntax.StaticKw is not null ? null : OwningType;
                var isMut = DeclarationSyntax.MutKw is not null;
                field = type is not null
                    ? new ParameterSymbol(Names.Self, owner: this, type, isMut, isSelf: true)
                    : null;
                _isSelfParameterBound = true;
            }

            return field;
        }
    }

    public override TypeSymbol ReturnType
    {
        get
        {
            if (field is null)
            {
                if (_isBindingReturnType)
                    throw new CycleException();
                _isBindingReturnType = true;

                _signatureBinder ??= MakeSignatureBinder();
                field = _signatureBinder.BindFunReturnType(DeclarationSyntax.ReturnTypeAnnotation);

                _isBindingReturnType = false;
            }

            return field;
        }
    }

    public override FunBody Body
    {
        get
        {
            if (field.Value is null)
            {
                if (_isBindingBody)
                    throw new CycleException();
                _isBindingBody = true;

                _signatureBinder ??= MakeSignatureBinder();
                field = intrinsicAnnotation is not null
                    ? _signatureBinder.BindIntrinsicFunBody(DeclarationSyntax.Body, intrinsicAnnotation)
                    : _signatureBinder.BindSourceFunBody(DeclarationSyntax.Body);

                _isBindingBody = false;
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = Parameters;
        _ = ReturnType;
        _ = Body;
        
        _signatureBinder!.SideEffects.DrainInto(sideEffects);
        _signatureBinder = null;
    }

    private Binder MakeSignatureBinder()
        => new(enclosingScope.WithOwner(this), new BinderSideEffects());
}