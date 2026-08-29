using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FileScopedModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : BaseModuleDeclSyntax(SyntaxKind.FileScopedModuleDecl, children);