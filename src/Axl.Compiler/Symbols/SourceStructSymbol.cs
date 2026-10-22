using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public sealed class SourceStructSymbol(
    StructDeclSyntax declarationSyntax,
    Scope enclosingScope,
    bool isPublic,
    bool isPrimitive)
    : StructSymbol(declarationSyntax.Name.Text.ToString(), owner: enclosingScope.Owner, isPublic, isPrimitive)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _binder = null;
    private bool _isBindingMembers = false;
    
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

                _binder ??= MakeBinder();
                field = DeclarationSyntax.Body is null ? [] :
                    [
                        .. DeclarationSyntax.Body.SyntaxNodes().Select(syntax => syntax switch
                        {
                            FieldDeclSyntax fieldDecl => (Symbol)_binder.BindFieldDeclaration(fieldDecl),
                            FunDeclSyntax funDecl => _binder.BindSourceFunDeclaration(funDecl),
                            _ => throw new UnreachableException($"Unknown struct member syntax {syntax.GetType().Name}")

                        })
                    ];

                _isBindingMembers = false;
                
                // The following checks accesses members, so they need to run
                // after binding is done.
                _binder.CheckDuplicateDeclarations(field);
                _binder.CheckRecursiveStructLayout(this);
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);

        _binder!.SideEffects.DrainInto(sideEffects);
        _binder = null;
    }

    private Binder MakeBinder()
        => new(new MemberScope(enclosingScope, this), new BinderSideEffects());

}