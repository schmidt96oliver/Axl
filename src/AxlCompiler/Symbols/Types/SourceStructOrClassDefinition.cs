using System.Collections.Immutable;
using System.Diagnostics;
using AxlCompiler.Binding;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

/// <summary>
/// Struct or class declared in source. All binding logic is shared,
/// the kinds only differ in semantics.
/// </summary>
public closed class SourceStructOrClassDefinition(
    StructOrClassDeclSyntax declarationSyntax,
    Scope enclosingScope,
    bool isPublic,
    PrimitiveKind? primitiveKind)
    : TypeDefinition(enclosingScope.Owner.Compilation,
        declarationSyntax.Name.Text.ToString(),
        owner: enclosingScope.Owner,
        isPublic: isPublic,
        primitiveKind: primitiveKind)
{
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingMembers = false;
    private bool _isBindingTypeParameters = false;

    public StructOrClassDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [DeclarationSyntax];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [DeclarationSyntax.Name];

    /// <inheritdoc />
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

                var binder = new Binder(enclosingScope.WithOwner(Identity), _sideEffects);
                field = binder.BindTypeParameters(DeclarationSyntax.TypeParameterList);

                _isBindingTypeParameters = false;
            }

            return field;
        }
    }

    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (field.IsDefault)
            {
                if (_isBindingMembers)
                    throw new CycleException();
                _isBindingMembers = true;

                var binder = new Binder(new MemberScope(enclosingScope, Identity), _sideEffects);
                field = DeclarationSyntax.Body is null ? [] :
                [
                    .. DeclarationSyntax.Body.SyntaxNodes().Select(syntax => syntax switch
                    {
                        FieldDeclSyntax fieldDecl => (Symbol)binder.BindFieldDeclaration(fieldDecl),
                        FunDeclSyntax funDecl => binder.BindSourceFunDeclaration(funDecl),
                        ConstDeclSyntax constDecl => binder.BindConstDeclaration(constDecl),
                        _ => throw new UnreachableException($"Unknown type member syntax {syntax.GetType().Name}")
                    })
                ];

                _isBindingMembers = false;

                // The following checks accesses members, so they need to run
                // after binding is done.
                binder.CheckDuplicateDeclarations([.. field, .. TypeParameters]);
                CheckMembers(binder);
            }

            return field;
        }
    }

    /// <summary>
    /// Kind specific checks after members have been bound.
    /// </summary>
    protected virtual void CheckMembers(Binder binder)
    {
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = TypeParameters;
        foreach (var member in Members)
            member.Complete(sideEffects);

        sideEffects.MergeWith(_sideEffects);
    }
}