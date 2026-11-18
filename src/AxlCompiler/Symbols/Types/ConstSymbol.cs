using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public sealed class ConstSymbol : Symbol
{
    private readonly bool _isIdentity;

    internal ConstSymbol(ConstDefinition definition, Symbol owner, bool isIdentity)
        : base(definition.Compilation, definition.Name, owner)
    {
        Definition = definition;
        Owner = owner;
        _isIdentity = isIdentity;
    }

    public ConstDefinition Definition { get; }

    /// <inheritdoc />
    public override Symbol Owner { get; }

    public override ConstSymbol OriginalDefinition => Definition.Identity;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => Definition.CanBeReferencedByName;

    public override bool IsPublic => Definition.IsPublic;

    /// <inheritdoc />
    public override bool IsConcrete => Owner.IsConcrete && Type.IsConcrete;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => Definition.DeclaringSyntax;

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => Definition.NameSyntax;

    public TypeSymbol Type
    {
        get
        {
            if (_isIdentity)
                return Definition.Type;
            field ??= Definition.Type.Substitute(TypeMap.For(this));
            return field;
        }
    }

    public FunSymbol ValueFun
    {
        get
        {
            field ??= Definition.ValueFun.WithOwner(Owner);
            return field;
        }
    }

    public override ConstSymbol Substitute(TypeMap map)
        => Definition.Specialize(Owner.Substitute(map), []);

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        if (_isIdentity)
            Definition.Complete(sideEffects);
    }
}