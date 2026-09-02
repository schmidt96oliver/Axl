using System.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// Represents one part of a module declaration.
/// </summary>
/// <example>
/// module A.B.C;
/// will produce:
/// Prefix ("A")
///    -> Prefix ("B")
///       -> Body("C")
/// </example>
public abstract record ModuleFragment(SymbolName Name)
{
    /// <summary>
    /// This fragment has no members, just one child fragment.
    /// </summary>
    public sealed record Prefix(SymbolName Name, ModuleFragment Child) : ModuleFragment(Name);

    /// <summary>
    /// This fragment contains the members for the module declaration.
    /// </summary>
    public sealed record Body(SymbolName Name, ModuleDeclSyntax Syntax) : ModuleFragment(Name)
    {
        /// <summary>
        /// All nodes of <see cref="Syntax"/>s parent excluding
        /// <see cref="Syntax"/> itself.
        /// </summary>
        public IEnumerable<SyntaxNode> Nodes
        {
            get
            {
                Debug.Assert(Syntax.Parent is FileSyntax, $"{nameof(ModuleDeclSyntax)} is on {nameof(FileSyntax)}");
                return ((FileSyntax)Syntax.Parent!)
                    .SyntaxNodes()
                    .Where(node => node != Syntax);
            }
        }
    }


    public static ModuleFragment FromDeclaration(ModuleDeclSyntax syntax)
    {
        var pathParts = syntax.Path.Parts.ToList();
        Debug.Assert(pathParts.Count >= 1, $"All {nameof(PathSyntax)} have at least one part.");

        // Work all path parts backwards.
        // module A.B.C.D;
        
        ModuleFragment fragment = new Body(SymbolName.From(pathParts[^1]), syntax);
        for (var i = pathParts.Count - 2; i >= 0; i--)
            fragment = new Prefix(SymbolName.From(pathParts[i]), fragment);

        return fragment;
    }
}