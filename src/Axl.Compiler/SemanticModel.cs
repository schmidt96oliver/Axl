using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler;

public sealed class SemanticModel(Compilation compilation)
{
    /// <summary>
    /// Returns all symbols that are visible at the start of the given
    /// <paramref name="location"/>. All symbols that are returned
    /// will be bound correctly, given a valid continuation.
    /// </summary>
    public ImmutableArray<Symbol> VisibleSymbolsAt(SourceLocation location)
        => VisibleSymbolsAt(compilation.GetSyntaxTree(location.SourceText), location.Start);

    /// <summary>
    /// Returns all symbols that are visible at the given
    /// <paramref name="position"/> inside the given <paramref name="tree"/>.
    /// All symbols that are returned will be bound correctly, given a
    /// valid continuation.
    /// </summary>
    public ImmutableArray<Symbol> VisibleSymbolsAt(SyntaxTree tree, int position)
    {
        const string probeText = "__AXL_HOH_HOH_CARET__";
        
        Guard.MustBe(tree.SourceText.Range.Contains(position) || position == tree.SourceText.Range.End);

        // We work by forking the compilation and inserting a probe. The binder
        // cooperates and returns visible symbols through its side effects,
        // if it saw the probe.

        var textWithProbe = SourceText.From(tree.SourceText.Text.Insert(position, probeText));
        var treeWithProbe = Parser.Parse(textWithProbe);
        var probeToken = TryFindProbeToken(treeWithProbe.FileSyntax, position, probeText)
                         ?? throw new UnreachableException(
                             "Could not find probe token in the syntax tree we prepared.");

        var compilationWithProbe = compilation
            .Replace(tree, with: treeWithProbe)
            .WithProbe(probeToken);

        var sideEffects = new BinderSideEffects();
        
        //TODO: Complete more fine-grained instead of just binding the entire fucking world
        compilationWithProbe.Complete(sideEffects);

        return sideEffects.VisibleSymbolsAtProbe ?? [];
    }

    private static Token? TryFindProbeToken(SyntaxNode node, int position, string probeText)
    {
        // The probe will start somewhere to the right of the given
        // position, but doesn't have to start exactly there. E.g.
        // it could be inside another identifier as in `abcPROBEdef`.

        foreach (var child in node.Children)
        {
            // Trivia is not relevant.
            if (child.Range is null) continue;
            if (!child.Range.Value.Contains(position))
                continue;
            if (child is IdentifierToken idToken &&
                idToken.Text.Contains(probeText, StringComparison.InvariantCulture))
            {
                return idToken;
            }

            if (child is SyntaxNode childNode && 
                TryFindProbeToken(childNode, position, probeText) is { } probeToken)
            {
                return probeToken;
            }
        }

        return null;
    }
}