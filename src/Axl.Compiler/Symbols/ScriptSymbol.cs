using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Symbols;

public class ScriptSymbol(SyntaxTree syntax, Scope enclosingScope) 
    : NamespaceSymbol(enclosingScope.Compilation, name: "", owner: null, isRoot: false)
{
    private readonly BinderSideEffects _sideEffects = new();
    private LazyUsingsScope? _lazyUsingsScope;
    private bool _isBindingMembers;
    private bool _isBindingScriptFun;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [syntax.FileSyntax];

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

                var binder = new Binder(new MemberScope(enclosingScope, this), _sideEffects);
                field = binder.BindScriptMembers(syntax.FileSyntax, out _lazyUsingsScope);
                
                _isBindingMembers = false;

                binder.CheckDuplicateDeclarations(field);
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
                
                // Trigger Member completion, so we get the usings scope
                _ = Members;
                Debug.Assert(_lazyUsingsScope is not null);
                
                var binder = new Binder(_lazyUsingsScope, _sideEffects);

                field = new SyntheticFunSymbol(name: "", owner: this, isPublic: false);
                field.SetSignature(selfParameter: null, [], binder.Scope.Compilation.BaseLibrary.Unit);
                field.SetBody(binder.EnterFunSignature(field).BindScriptBody(syntax.FileSyntax));

                _isBindingScriptFun = false;
            }

            return field;
        }
    }

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);
        _ = Fun;
        _lazyUsingsScope?.Complete(sideEffects);
        
        sideEffects.AddFrom(_sideEffects);
    }
}