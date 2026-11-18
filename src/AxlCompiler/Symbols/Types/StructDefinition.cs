using AxlCompiler.Binding;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class StructDefinition(
    StructOrClassDeclSyntax declarationSyntax,
    Scope enclosingScope,
    bool isPublic,
    PrimitiveKind? primitiveKind)
    : SourceStructOrClassDefinition(declarationSyntax, enclosingScope, isPublic, primitiveKind)
{
    public override bool IsValueType => true;
    public override bool IsReferenceType => false;

    protected override void CheckMembers(Binder binder)
        => binder.CheckRecursiveStructLayout(Identity);
}