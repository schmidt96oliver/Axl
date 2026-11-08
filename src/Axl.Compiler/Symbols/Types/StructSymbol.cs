using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax;
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
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingMembers = false;
    private bool _isBindingTypeParameters = false;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [DeclarationSyntax];
    
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
                // Add type parameters here.
                binder.CheckDuplicateDeclarations([.. field, .. TypeParameters]);
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = TypeParameters;
        foreach (var member in Members)
            member.Complete(sideEffects);
        
        // The layout graph check will access type members and therefore
        // run recursively inside each other. So just do it here, since
        // it's just a check that doesn't compute any real values.
        new Binder(new MemberScope(enclosingScope, this), _sideEffects).CheckRecursiveStructLayout(this);
        
        sideEffects.AddFrom(_sideEffects);
    }
}