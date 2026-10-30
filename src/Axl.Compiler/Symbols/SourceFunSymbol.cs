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
    : FunDefinitionSymbol(enclosingScope.Compilation, declarationSyntax.Name.Text.ToString(), owner: enclosingScope.Owner, isPublic: isPublic)
{
    public override FunDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    private BinderSideEffects _sideEffects = new();
    private bool _isBindingTypeParameters = false;
    private bool _isBindingParameters = false;
    private bool _isBindingReturnType = false;
    private bool _isBindingBody = false;
    private bool _isSelfParameterBound = false;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    public override ImmutableArray<TypeParameterSymbol> TypeParameters
    {
        get
        {
            if (field.IsDefault)
            {
                if (DeclarationSyntax.TypeParameterList is null)
                {
                    field = [];
                    return field;
                }
                
                if (_isBindingTypeParameters)
                    throw new CycleException();
                _isBindingTypeParameters = true;

                var binder = new Binder(enclosingScope.WithOwner(this), _sideEffects);
                field = binder.BindTypeParameters(DeclarationSyntax.TypeParameterList);

                _isBindingTypeParameters = false;
            }

            return field;
        }
    }

    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (field.IsDefault)
            {
                if (_isBindingParameters)
                    throw new CycleException();
                _isBindingParameters = true;
                
                var binder = MakeSignatureBinder();
                field = binder.BindParameters(DeclarationSyntax.ParameterList);

                _isBindingParameters = false;

                if (IsOperator)
                    binder.CheckOperatorSignature(this);
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

    private TypeSymbol? _returnType;
    private bool? _isReturnMut;
    
    public override TypeSymbol ReturnType
    {
        get
        {
            if (_returnType is null)
            {
                if (_isBindingReturnType)
                    throw new CycleException();
                _isBindingReturnType = true;

                (_returnType, _isReturnMut) = MakeSignatureBinder()
                    .BindFunReturnType(DeclarationSyntax.ReturnTypeAnnotation);

                _isBindingReturnType = false;
            }

            return _returnType;
        }
    }

    /// <inheritdoc />
    public override bool IsReturnMut
    {
        get
        {
            if (_isReturnMut is null)
            {
                if (_isBindingReturnType)
                    throw new CycleException();
                _isBindingReturnType = true;

                (_returnType, _isReturnMut) =
                    MakeSignatureBinder().BindFunReturnType(DeclarationSyntax.ReturnTypeAnnotation);

                _isBindingReturnType = false;
            }

            return _isReturnMut.Value;
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

                field = intrinsicAnnotation is not null
                    ? MakeSignatureBinder().BindIntrinsicFunBody(DeclarationSyntax.Body, intrinsicAnnotation)
                    : MakeSignatureBinder().BindSourceFunBody(DeclarationSyntax.Body);

                _isBindingBody = false;
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = TypeParameters;
        _ = Parameters;
        _ = ReturnType;
        _ = Body;
        
        _sideEffects.DrainInto(sideEffects);
    }

    private Binder MakeSignatureBinder()
        => new(new FunSignatureScope(enclosingScope.WithOwner(this)), _sideEffects);
}