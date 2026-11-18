using AxlCompiler.Binding.Scopes;
using AxlCompiler.Syntax.Tree;

namespace AxlCompiler.Symbols;

public sealed class ClassDefinition(
    StructOrClassDeclSyntax declarationSyntax,
    Scope enclosingScope,
    bool isPublic,
    PrimitiveKind? primitiveKind)
    : SourceStructOrClassDefinition(declarationSyntax, enclosingScope, isPublic, primitiveKind)
{
    public override bool IsValueType => false;
    public override bool IsReferenceType => true;
}