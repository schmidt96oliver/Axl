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
    private readonly Dictionary<(SyntaxTree, int), BinderSideEffects?> _probes = [];
    
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


    public Scope? TryScopeAtUnmapped(SourceLocation location)
        => TryScopeAtUnmapped(compilation.SyntaxTreeFor(location.SourceText), location.Start);
    
    public Scope? TryScopeAtUnmapped(SyntaxTree tree, int position)
        => ProbeAt(tree, position)?.ScopeAtProbe;


    public BoundNode BoundNodeFor(ExprSyntax syntax) 
        => TryBoundNodeFor(syntax) ?? new BoundError(compilation.BaseLibrary.Error, syntax);

    private BoundNode? TryBoundNodeFor(ExprSyntax syntax)
    {
        //TODO: Cache the results
        var sideEffects = new BinderSideEffects();
        CompleteAt(syntax.Location, compilation, sideEffects);

        if (sideEffects.BoundNodes.TryGetValue(syntax, out var boundNodes))
            return boundNodes[^1]; //TODO: Which one to pick?

        return null;
    }

    public SymbolInfo ReferencedSymbolFor(NameRefSyntax syntax)
        => InternalReferencedSymbolFor(syntax) ?? new SymbolInfo.Unresolved([], UnresolvedSymbolReason.NotFound);
    
    /// <summary>
    /// Returns the referenced symbol, but <c>null</c> if it
    /// could not be resolved due to an internal error. Used for
    /// testing.
    /// </summary>
    internal SymbolInfo? InternalReferencedSymbolFor(NameRefSyntax syntax)
    {
        // Annotations are no symbols
        if (syntax.Parent is AnnotationSyntax)
            return new SymbolInfo.Unresolved([], UnresolvedSymbolReason.NotFound);
        
        //TODO: Handle argument names
        if (syntax.Parent is ArgSyntax arg && arg.Name == syntax)
            return null;
        
        return ReferencedSymbolFor(BindableSyntaxFor(syntax));
    }

    private SymbolInfo? ReferencedSymbolFor(ExprSyntax syntax)
        => TryBoundNodeFor(syntax) switch
        {
            IBoundSymbolRef symbolRef => new SymbolInfo.Resolved(symbolRef.Symbol),
            BoundTypeGroup typeGroup => ReferencedSymbolFor(typeGroup, syntax),
            BoundFunGroup funGroup => ReferencedSymbolFor(funGroup, syntax),
            BoundLookupError lookupError => new SymbolInfo.Unresolved(lookupError.Candidates, lookupError.Reason),
            _ => null
        };

    private SymbolInfo ReferencedSymbolFor(BoundTypeGroup typeGroup, ExprSyntax syntax)
    {
        if (syntax.Parent is CallExprSyntax callExprSyntax && callExprSyntax.Callee == syntax)
        {
            var boundCall = BoundNodeFor(callExprSyntax);
            if (boundCall is BoundStructInit structInit)
                return new SymbolInfo.Resolved(structInit.Type);
            if (boundCall is BoundStructInitError structInitError)
            {
                // If there is only a single candidate that has its type
                // parameters filled, it resolves correctly. Otherwise,
                // it's unresolved (too many types or failed inference).
                if (structInitError.Candidates is [{ HasFilledTypeParameters: true } singleType])
                    return new SymbolInfo.Resolved(singleType);

                return new SymbolInfo.Unresolved(
                    structInitError.Candidates.CastArray<Symbol>(),
                    UnresolvedSymbolReason.NoApplicableType);
            }
        }

        // Same as above
        if (typeGroup.Candidates is [{ HasFilledTypeParameters: true } single])
            return new SymbolInfo.Resolved(single);
        
        return new SymbolInfo.Unresolved(
            typeGroup.Candidates.CastArray<Symbol>(),
            UnresolvedSymbolReason.Ambiguous);
    }

    private SymbolInfo ReferencedSymbolFor(BoundFunGroup funGroup, ExprSyntax syntax)
    {
        var callSyntax = syntax.Ancestors().OfType<CallExprSyntax>().FirstOrDefault();
        if (callSyntax is null || callSyntax.Callee != syntax ||
            BoundNodeFor(callSyntax) is not BoundCall boundCall)
        {
            // The fun group is not part of a call or it had an error.
            return new SymbolInfo.Unresolved(funGroup.Candidates.CastArray<Symbol>(), UnresolvedSymbolReason.NoApplicableOverload);
        }

        return new SymbolInfo.Resolved(boundCall.Fun);
    }
    
    private ExprSyntax BindableSyntaxFor(ExprSyntax syntax)
    {
        return syntax.Parent switch
        {
            MemberAccessExprSyntax memberAccess when memberAccess.Member == syntax
                => BindableSyntaxFor(memberAccess),
            SubscriptExprSyntax subscript when subscript.Left == syntax
                => subscript,
            
            _ => syntax,
        };
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
        //TODO: Symbol.Traverse?
        
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