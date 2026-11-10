using System.Collections.Immutable;
using System.Diagnostics;
using AxlCompiler.Diagnostics;
using AxlCompiler.Binding;
using AxlCompiler.Binding.BoundTree;
using AxlCompiler.Binding.Scopes;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Text;

namespace AxlCompiler;

public sealed class SemanticModel(Compilation compilation)
{
    private Dictionary<(SyntaxTree, int), BinderSideEffects?> _probes = [];
    
    /// <summary>
    /// Returns all symbols that are visible at the start of the given
    /// <paramref name="location"/>. All symbols that are returned
    /// will be bound correctly, given a valid continuation.
    /// </summary>
    public ImmutableArray<Symbol> VisibleSymbolsAtUnmapped(SourceLocation location)
        => VisibleSymbolsAtUnmapped(compilation.SyntaxTreeFor(location.SourceText), location.Start);

    /// <summary>
    /// Returns representations of all symbols that are visible at the given
    /// <paramref name="position"/> inside the given <paramref name="tree"/>.
    /// The returned symbols will not be part of the original compilation and
    /// will not compare equal to their according symbols (hence, unmapped).
    /// All symbols that are returned will be bound correctly, given a
    /// valid continuation. 
    /// </summary>
    public ImmutableArray<Symbol> VisibleSymbolsAtUnmapped(SyntaxTree tree, int position)
        => ProbeAt(tree, position)?.VisibleSymbolsAtProbe ?? [];


    public Scope? TryScopeAt(SourceLocation location)
        => TryScopeAt(compilation.SyntaxTreeFor(location.SourceText), location.Start);
    
    public Scope? TryScopeAt(SyntaxTree tree, int position)
        => ProbeAt(tree, position)?.ScopeAtProbe;


    public BoundNode BoundNodeFor(ExprSyntax syntax)
    {
        //TODO: Cache the results
        var sideEffects = new BinderSideEffects();
        CompleteAt(syntax.Location, compilation, sideEffects);

        if (sideEffects.BoundNodes.TryGetValue(syntax, out var boundNodes))
            return boundNodes[^1];  //TODO: Which one to pick?
        else
            return new BoundError(compilation.BaseLibrary.Error, syntax);
    }
    
    
    private BinderSideEffects? ProbeAt(SyntaxTree tree, int position)
    {
        const string probeText = "__AXL_HOH_HOH_CARET__";

        if (_probes.TryGetValue((tree, position), out var probe))
            return probe;
        
        if (!tree.SourceText.Range.Contains(position) && position != tree.SourceText.Range.End)
        {
            _probes[(tree, position)] = null;
            return null;
        }

        // We work by forking the compilation and inserting a probe. The binder
        // cooperates and returns visible symbols through its side effects,
        // if it saw the probe.

        var textWithProbe = SourceText.From(tree.SourceText.Text.Insert(position, probeText));
        var treeWithProbe = Parser.Parse(textWithProbe);
        var probeToken = TryFindProbeToken(treeWithProbe.FileSyntax, position, probeText);
        if (probeToken is null)
        {
            // The probe did not parse as an identifier, which means that it would
            // not produce names anyway. E.g. in a comment or string.
            _probes[(tree, position)] = null;
            return null;
        }

        probe = new BinderSideEffects();
        var compilationWithProbe = compilation
            .Replace(tree, with: treeWithProbe)
            .WithProbe(probeToken);
        CompleteAt(probeToken.Location, compilationWithProbe, probe);

        _probes[(tree, position)] = probe;
        return probe;
    }
    
    private static void CompleteAt(SourceLocation location, Compilation compilation, BinderSideEffects sideEffects)
    {
        // Complete only the lowest most symbol that contains the probe,
        // as not to bind any unnecessary symbols.
        foreach (var symbol in compilation.Scripts.Concat([compilation.RootNamespace]))
        {
            if (!symbol.DeclaringSyntax.Any(declSyntax => declSyntax.Location.Contains(location)))
                continue;

            Symbol current = symbol;
            while (true)
            {
                var next = current.Members.FirstOrDefault(member =>
                    member.DeclaringSyntax.Any(declSyntax => declSyntax.Location.Contains(location)));
                if (next is null)
                {
                    current.Complete(sideEffects);
                    return;
                }

                current = next;
            }
        }
        
        // The loop did not complete anything. So fall back to completing the entire
        // compilation.
        compilation.Complete(sideEffects);
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