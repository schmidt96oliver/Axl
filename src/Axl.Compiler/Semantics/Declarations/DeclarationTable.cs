using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Declarations;

public sealed class DeclarationTable(ImmutableArray<SyntaxTree> trees)
{
    private readonly Dictionary<SyntaxTree, ModuleDeclFragment?> _fileDeclsByTree = [];

    public ModuleDecl GlobalDecl
    {
        get
        {
            field ??= new ModuleDecl(SymbolName.Empty, [
                .. trees
                    .Select(GetFileFragment)
                    .Where(frag => frag is not null)!
            ]);
            return field;
        }
    }
    
    private ModuleDeclFragment? GetFileFragment(SyntaxTree tree)
    {
        if (_fileDeclsByTree.TryGetValue(tree, out var fragment))
            return fragment;

        fragment = BuildFileFragment(tree);
        if (fragment is not null)
            _fileDeclsByTree.Add(tree, fragment);
        return fragment;
    }

    private ModuleDeclFragment? BuildFileFragment(SyntaxTree tree)
    {
        // Usings are irrelevant for the declaration table.
        var declNodes = tree.FileSyntax
            .SyntaxNodes()
            .Where(node => node.Kind is not SyntaxKind.UsingDirective)
            .ToList();

        var isScriptFile = declNodes.Any(node => node is not BaseModuleDeclSyntax) || declNodes.Count == 0;
        if (isScriptFile)
            return null;

        var diagnosticBag = new DiagnosticBag();
        var fragments = ImmutableArray.CreateBuilder<ModuleDeclFragment>();

        // Build first fragment separately, so we can reject file-scoped decls
        // later in the loop. File-scoped decls must always be the first.
        Debug.Assert(declNodes.Count > 0);
        fragments.Add(BuildModuleDeclFragment((BaseModuleDeclSyntax)declNodes[0]));
        
        foreach (var node in declNodes.Skip(1))
        {
            Debug.Assert(node is BaseModuleDeclSyntax);

            if (node is FileScopedModuleDeclSyntax fileScopedSyntax)
                diagnosticBag.ReportError(new Diagnostic.InvalidFileScopedModuleDecl(fileScopedSyntax));
            
            fragments.Add(BuildModuleDeclFragment((BaseModuleDeclSyntax)node));
        }

        return new ModuleDeclFragment(SymbolName.Empty,
            Syntax: null,
            fragments.DrainToImmutable(),
            diagnosticBag.Drain());
    }
    
    private ModuleDeclFragment BuildModuleDeclFragment(BaseModuleDeclSyntax syntax)
    {
        // Report diagnostic for all file-scoped declarations.
        // The parser closes them immediately, so they do not
        // contain anything relevant. Do not add a declaration
        // for them.
        var diagnosticBag = new DiagnosticBag();
        foreach (var fileScopedDeclSyntax in syntax.Members.OfType<FileScopedModuleDeclSyntax>())
        {
            diagnosticBag.ReportError(new Diagnostic.InvalidFileScopedModuleDecl(fileScopedDeclSyntax));
        }
        
        // Visit all children module decls
        var childModuleDecls = syntax.Members
            .OfType<ModuleDeclSyntax>()
            .Select(BuildModuleDeclFragment)
            .ToImmutableArray();
        
        // Reduce dotted paths as in
        // `module A.B.C`
        var pathNameParts = syntax.Name.Parts.ToList();
        Debug.Assert(pathNameParts.Count >= 1, $"Parser must emit at least one part for {nameof(PathSyntax)}");

        var currentSyntax = syntax;
        for (var pathPart = pathNameParts.Count - 1; pathPart >= 1; pathPart--)
        {
            var decl = new ModuleDeclFragment(SymbolName.From(pathNameParts[pathPart]), 
                currentSyntax,
                childModuleDecls, 
                Diagnostics: diagnosticBag?.Drain() ?? []);
            
            childModuleDecls = [decl];
            currentSyntax = null;
            diagnosticBag = null;
        }
        
        return new ModuleDeclFragment(SymbolName.From(pathNameParts[0]), 
            currentSyntax, 
            childModuleDecls, 
            Diagnostics: diagnosticBag?.Drain() ?? []);
    }
}