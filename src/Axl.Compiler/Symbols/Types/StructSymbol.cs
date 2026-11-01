using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class StructSymbol(
    StructDeclSyntax declarationSyntax,
    Scope enclosingScope,
    bool isPublic,
    PrimitiveKind? primitiveKind)
    : TypeDefinitionSymbol(enclosingScope.Owner.Compilation,
        declarationSyntax.Name.Text.ToString(), 
        owner: enclosingScope.Owner, 
        isPublic: isPublic, 
        primitiveKind: primitiveKind)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingMembers = false;
    private bool _isBindingTypeParameters = false;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    public ImmutableArray<FieldSymbol> Fields => [.. Members.OfType<FieldSymbol>()];


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

                var binder = new Binder(enclosingScope.WithOwner(this), _sideEffects);
                field = binder.BindTypeParameters(DeclarationSyntax.TypeParameterList);

                _isBindingTypeParameters = false;
            }

            return field;
        }
    }
    
    public StructDeclSyntax DeclarationSyntax { get; } = declarationSyntax;

    public override ImmutableArray<Symbol> Members
    {
        get
        {
            if (field.IsDefault)
            {
                if (_isBindingMembers)
                    throw new CycleException();
                _isBindingMembers = true;

                var binder = new Binder(new MemberScope(enclosingScope, this), _sideEffects);
                field = DeclarationSyntax.Body is null ? [] :
                    [
                        .. DeclarationSyntax.Body.SyntaxNodes().Select(syntax => syntax switch
                        {
                            FieldDeclSyntax fieldDecl => (Symbol)binder.BindFieldDeclaration(fieldDecl),
                            FunDeclSyntax funDecl => binder.BindSourceFunDeclaration(funDecl),
                            _ => throw new UnreachableException($"Unknown struct member syntax {syntax.GetType().Name}")

                        })
                    ];

                _isBindingMembers = false;
                
                // The following checks accesses members, so they need to run
                // after binding is done.
                binder.CheckDuplicateDeclarations(field);
                binder.CheckRecursiveStructLayout(this);
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = TypeParameters;
        foreach (var member in Members)
            member.Complete(sideEffects);

        _sideEffects.DrainInto(sideEffects);
    }
}