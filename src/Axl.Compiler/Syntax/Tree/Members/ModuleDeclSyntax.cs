using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ModuleDeclSyntax(ImmutableArray<SyntaxElement> children)
    : BaseModuleDeclSyntax(SyntaxKind.ModuleDecl, children);