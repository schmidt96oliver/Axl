using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public partial class SymbolTableBuilder
{
    private record ModuleDecl(SymbolName? Name, List<ModuleDeclSyntax> Syntaxes, List<ModuleDecl> Children);

    private class ModuleDeclBuilder
    {
        private ImmutableArray<ModuleDecl>.Builder _topLevelDecls 
            = ImmutableArray.CreateBuilder<ModuleDecl>();

        private ModuleDeclBuilder()
        {
            
        }
        
        public static ImmutableArray<ModuleDecl> Build(ImmutableArray<SyntaxTree> trees)
        {
            var builder = new ModuleDeclBuilder();
            
            foreach (var tree in trees)
            foreach (var moduleDeclSyntax in tree.FileSyntax.Children.OfType<ModuleDeclSyntax>())
                builder.VisitModuleSyntax(moduleDeclSyntax, parent: null);

            return builder._topLevelDecls.DrainToImmutable();
        }
        
        
        private void VisitModuleSyntax(ModuleDeclSyntax syntax, ModuleDecl? parent)
        {
            var moduleDecl = GetDeclFromSyntax(syntax, parent);

            moduleDecl.Syntaxes.Add(syntax);

            foreach (var childModuleSyntax in syntax.Members.OfType<ModuleDeclSyntax>())
                VisitModuleSyntax(childModuleSyntax, parent: moduleDecl);
        }
        
        private ModuleDecl GetDeclFromSyntax(ModuleDeclSyntax syntax, ModuleDecl? parent)
        {
            var partNames = syntax.Name.Parts
                .Select(token => token.IsMissing ? (SymbolName?)null : SymbolName.From(token)
                ).ToList();

            Debug.Assert(partNames.Count >= 1,
                "Path had no parts. At least one should've been synthesized by the parser.");

            var currentDecl = parent;
            foreach (var partName in partNames)
                currentDecl = GetOrCreateDecl(partName, parent: currentDecl);
            return currentDecl!;
        }
        
        private ModuleDecl GetOrCreateDecl(SymbolName? name, ModuleDecl? parent)
        {
            if (name is not null)
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