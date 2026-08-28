using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public partial class SymbolTableBuilder
{
    private record ModuleDecl(SymbolName Name, List<NormalOrFileScopedModuleDeclSyntax> Syntaxes, List<ModuleDecl> Children);

    private class ModuleDeclBuilder
    {
        private readonly ImmutableArray<ModuleDecl>.Builder _topLevelDecls 
            = ImmutableArray.CreateBuilder<ModuleDecl>();

        private ModuleDeclBuilder()
        {
            
        }
        
        public static ImmutableArray<ModuleDecl> Build(ImmutableArray<SyntaxTree> trees)
        {
            var builder = new ModuleDeclBuilder();
            
            // Only visit module files
            foreach (var tree in trees.Where(tree => tree.GetAxlFileKind() is AxlFileKind.ModuleFile))
            {
                var allowFileScopedDecl = true;
                ModuleDecl? fileScopedModuleParent = null;
                
                foreach (var memberSyntax in tree.FileSyntax.Members)
                {
                    if (memberSyntax is FileScopedModuleDeclSyntax fileScopedDeclSyntax)
                    {
                        // Just skip additional file-scoped declarations. They will be 
                        // reported be symbol table building later.
                        if (!allowFileScopedDecl)
                            continue;

                        fileScopedModuleParent = builder.VisitFileScopedModuleSyntax(fileScopedDeclSyntax);

                        // Allow only one file-scoped declaration
                        allowFileScopedDecl = false;
                    }
                    else if (memberSyntax is ModuleDeclSyntax moduleDeclSyntax)
                    {
                        builder.VisitModuleSyntax(moduleDeclSyntax, parent: fileScopedModuleParent);

                        // File-scoped declarations may not come after module
                        // declarations.
                        allowFileScopedDecl = false;
                    }
                }
            }

            return builder._topLevelDecls.DrainToImmutable();
        }

        private ModuleDecl VisitFileScopedModuleSyntax(FileScopedModuleDeclSyntax syntax)
        {
            var moduleDecl = GetDeclFromPath(syntax.Name, parent: null);
            moduleDecl.Syntaxes.Add(syntax);
            return moduleDecl;
        }
        
        private void VisitModuleSyntax(ModuleDeclSyntax syntax, ModuleDecl? parent)
        {
            var moduleDecl = GetDeclFromPath(syntax.Name, parent);

            moduleDecl.Syntaxes.Add(syntax);

            foreach (var childModuleSyntax in syntax.Members.OfType<ModuleDeclSyntax>())
                VisitModuleSyntax(childModuleSyntax, parent: moduleDecl);
        }
        
        private ModuleDecl GetDeclFromPath(PathSyntax pathSyntax, ModuleDecl? parent)
        {
            var partNames = pathSyntax.Parts
                .Select(SymbolName.From);

            var currentDecl = parent;
            foreach (var partName in partNames)
                currentDecl = GetOrCreateDecl(partName, parent: currentDecl);
            
            Debug.Assert(currentDecl != parent, $"{nameof(pathSyntax)} has no parts.");
            Debug.Assert(currentDecl is not null);
            
            return currentDecl;
        }
        
        private ModuleDecl GetOrCreateDecl(SymbolName name, ModuleDecl? parent)
        {
            // Don't search empty names. They will always get their own decl.
            if (!name.IsEmpty)
            {
                var moduleDecl = parent is null
                    ? _topLevelDecls.FirstOrDefault(decl => decl.Name == name)
                    : parent.Children.FirstOrDefault(decl => decl.Name == name);
                if (moduleDecl is not null)
                    return moduleDecl;
            }

            var decl = new ModuleDecl(name, [], []);
            if (parent is null)
                _topLevelDecls.Add(decl);
            else
                parent.Children.Add(decl);
                
            return decl;
        }
    }
}