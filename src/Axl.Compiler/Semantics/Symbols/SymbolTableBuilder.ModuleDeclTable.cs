using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public partial class SymbolTableBuilder
{
    public record ModuleDecl(SymbolName? Name, ModuleDecl? Parent, List<ModuleDeclSyntax> Syntaxes);

    public class ModuleDeclTable
    {
        private readonly Dictionary<ModuleDeclSyntax, ModuleDecl> _moduleDeclBySyntax = [];

        private ModuleDeclTable()
        {
            
        }
        
        
        public ModuleDecl GetModuleDecl(ModuleDeclSyntax syntax)
            => _moduleDeclBySyntax.TryGetValue(syntax, out var decl) 
                ? decl 
                : throw new ArgumentException("No module declared for given syntax.", nameof(syntax));

        public static ModuleDeclTable Build(ImmutableArray<SyntaxTree> trees)
        {
            var table = new ModuleDeclTable();

            foreach (var tree in trees)
            foreach (var moduleDeclSyntax in tree.FileSyntax.Children.OfType<ModuleDeclSyntax>())
                VisitModuleSyntax(moduleDeclSyntax, parent: null);

            return table;

            ModuleDecl GetOrCreate(SymbolName? name, ModuleDecl? parent)
            {
                if (name is not null)
                {
                    var moduleDecl = table._moduleDeclBySyntax.Values.Where(decl =>
                        decl.Parent == parent && decl.Name == name
                    );
                    if (moduleDecl.FirstOrDefault() is ModuleDecl foundDecl)
                        return foundDecl;
                }

                var decl = new ModuleDecl(name, parent, []);
                return decl;
            }

            ModuleDecl GetDeclFromSyntax(ModuleDeclSyntax syntax, ModuleDecl? parent)
            {
                var partNames = syntax.Name.Parts
                    .Select(token => token.IsMissing ? (SymbolName?)null : SymbolName.From(token)
                    ).ToList();

                Debug.Assert(partNames.Count >= 1,
                    "Path had no parts. At least one should've been synthesized by the parser.");

                var currentDecl = parent;
                foreach (var partName in partNames)
                    currentDecl = GetOrCreate(partName, parent: currentDecl);
                return currentDecl!;
            }

            void VisitModuleSyntax(ModuleDeclSyntax syntax, ModuleDecl? parent)
            {
                var moduleDecl = GetDeclFromSyntax(syntax, parent);

                moduleDecl.Syntaxes.Add(syntax);
                table._moduleDeclBySyntax.Add(syntax, moduleDecl);

                foreach (var childModuleSyntax in syntax.Members.OfType<ModuleDeclSyntax>())
                    VisitModuleSyntax(childModuleSyntax, parent: moduleDecl);
            }
        }
    }
}