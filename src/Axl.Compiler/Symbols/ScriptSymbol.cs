using System.Collections.Immutable;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public class ScriptSymbol(SyntaxTree syntax, Scope enclosingScope) 
    : NamespaceSymbol(name: "", owner: null, isRoot: false)
{
    /// <summary>
    /// To avoid unnecessary allocations, the binder is created on first
    /// use and released again after call to <see cref="Complete"/>.
    /// </summary>
    private Binder? _binder;
    private bool _isBindingMembers;
    private bool _isBindingScriptFun;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;
    
    /// <inheritdoc />
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
                field =
                [
                    .. syntax.FileSyntax.Children.OfType<StructDeclSyntax>().Select(_binder.BindStructDeclaration)
                ];
                
                _isBindingMembers = false;

                _binder.CheckDuplicateDeclarations(field);
            }

            return field;
        } 
    }

    public SyntheticFunSymbol Fun
    {
        get
        {
            if (field is null)
            {
                if (_isBindingScriptFun)
                    throw new CycleException();
                _isBindingScriptFun = true;
                
                _binder ??= MakeBinder();

                field = new SyntheticFunSymbol(name: "",
                    owner: this,
                    isPublic: false,
                    receiverType: null,
                    isMutatingReceiver: false);
                field.SetSignature([], _binder.Scope.Compilation.BaseLibrary.Unit);
                field.SetBody(_binder.EnterFunSignature(field).BindScriptBody(syntax.FileSyntax));

                _isBindingScriptFun = false;
            }

            return field;
        }
    }

    private Binder MakeBinder()
        => new(new MemberScope(enclosingScope, this), new BinderSideEffects());

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);
        _ = Fun;
        
        _binder!.SideEffects.DrainInto(sideEffects);
        _binder = null;
    }
}