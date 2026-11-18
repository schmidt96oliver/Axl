using System.Collections.Immutable;
using AxlCompiler.Binding;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public sealed class FieldSymbol : Symbol
{
    private readonly bool _isIdentity;

    internal FieldSymbol(FieldDefinition definition, TypeSymbol owner, bool isIdentity)
        : base(definition.Compilation, definition.Name, owner)
    {
        Definition = definition;
        Owner = owner;
        _isIdentity = isIdentity;
    }

    public FieldDefinition Definition { get; }

    public override TypeSymbol Owner { get; }

    public override FieldSymbol OriginalDefinition => Definition.Identity;

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

    public override FieldSymbol Substitute(TypeMap map)
        => Definition.Specialize(Owner.Substitute(map), []);

    /// <inheritdoc />
    public override void Complete(BinderSideEffects sideEffects)
    {
        if (_isIdentity)
            Definition.Complete(sideEffects);
    }
}