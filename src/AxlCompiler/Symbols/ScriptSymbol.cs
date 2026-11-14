using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Binding;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

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
    public override ImmutableArray<Token> NameSyntax => [];

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

    public ScriptFunSymbol Fun
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

                field = new ScriptFunSymbol(syntax.FileSyntax, _lazyUsingsScope);
                
                _isBindingScriptFun = false;
            }

            return field;
        }
    }

    /// <inheritdoc />
    protected virtual IEnumerable<Symbol> MembersForTraversal()
        // Members first, then the fun so members are found first.
        // This is better, because the fun will span the entire file syntax
        // and searching that way, will find the members first.
        => [.. Members, Fun];
    
    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        foreach (var member in Members)
            member.Complete(sideEffects);
        Fun.Complete(sideEffects);
        _lazyUsingsScope?.Complete(sideEffects);
        
        sideEffects.MergeWith(_sideEffects);
    }
}