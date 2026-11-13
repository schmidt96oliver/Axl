using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class ScriptFunSymbol(
    FileSyntax scriptSyntax,
    Scope enclosingScope)
    : FunDefinitionSymbol(enclosingScope.Compilation, "", owner: enclosingScope.Owner, isPublic: false)
{
    public FileSyntax ScriptSyntax { get; } = scriptSyntax;
    
    private readonly BinderSideEffects _sideEffects = new();
    private bool _isBindingBody = false;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    /// <inheritdoc />
    public override FunDeclSyntax? DeclarationSyntax => null;
    
    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [ScriptSyntax];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [];

    public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    public override ImmutableArray<ParameterSymbol> Parameters => [];

    /// <inheritdoc />
    public override ParameterSymbol? SelfParameter => null;

    public override TypeSymbol ReturnType => Compilation.BaseLibrary.Unit;

    /// <inheritdoc />
    public override bool IsReturnMut => false;

    

    public override FunBody Body
    {
        get
        {
            if (field.Value is null)
            {
                if (_isBindingBody)
                    throw new CycleException();
                _isBindingBody = true;

                field = new Binder(new FunSignatureScope(enclosingScope.WithOwner(this)), _sideEffects)
                    .BindScriptBody(ScriptSyntax);

                _isBindingBody = false;
            }

            return field;
        }
    }

    public override void Complete(BinderSideEffects sideEffects)
    {
        _ = Body;
        
        sideEffects.MergeWith(_sideEffects);
    }
}