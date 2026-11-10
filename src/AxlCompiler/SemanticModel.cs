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


    public Scope? TryScopeAtUnmapped(SourceLocation location)
        => TryScopeAtUnmapped(compilation.SyntaxTreeFor(location.SourceText), location.Start);
    
    public Scope? TryScopeAtUnmapped(SyntaxTree tree, int position)
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

    public Symbol? ReferencedSymbolFor(IdNameSyntax syntax)
    {
        var bindableSyntax = BindableSyntaxFor(syntax);
        if (bindableSyntax is null) return null;

        return ReferencedSymbolFor(bindableSyntax);
    }

    private Symbol? ReferencedSymbolFor(ExprSyntax syntax)
        => BoundNodeFor(syntax) switch
        {
            IBoundSymbolRef symbolRef => symbolRef.Symbol,
            BoundTypeGroup typeGroup => ReferencedSymbolFor(typeGroup, syntax),
            BoundFunGroup funGroup => ReferencedSymbolFor(funGroup, syntax),
            _ => null
        };

    private Symbol? ReferencedSymbolFor(BoundTypeGroup typeGroup, ExprSyntax syntax)
    {
        if (typeGroup.Candidates is [var single])
            return single;
        
        // Type groups are specialized through call expressions otherwise
        if (syntax.Ancestors().FirstOfTypeOrNull<CallExprSyntax>() is { } callExprSyntax)
        {
            var boundCall = BoundNodeFor(callExprSyntax);
            if (boundCall is BoundStructInit structInit)
                return structInit.Type;
            if (boundCall is BoundStructInitError structInitError)
                return structInitError.Candidates.Length == 1 ? structInitError.Candidates[0] : null;
        }

        return null;    // Ambiguous
    }

    private Symbol? ReferencedSymbolFor(BoundFunGroup funGroup, ExprSyntax syntax)
    {
        // If it's a single one, we can return it directly
        // if it needs no generic type inference.
        if (funGroup.Candidates is [var single] && single.TypeParameters.Length == funGroup.TypeArguments.Length)
        {
            return single.TypeParameters.Length > 0
                ? single.Specialize([.. funGroup.TypeArguments.Select(t => t.Type)])
                : single;
        }

        // Bind the overlaying call and return it's callee
        var callSyntax = syntax.Ancestors().OfType<CallExprSyntax>().FirstOrDefault();
        if (callSyntax is null || callSyntax.Callee != syntax)
            return funGroup.Candidates is [var singleFun]
                ? singleFun
                : null; //TODO: Return candidates (then also add subscript)
        var boundCall = BoundNodeFor(callSyntax) as BoundCall;
        if (boundCall is null)
            return funGroup.Candidates is [var singleFun] ? singleFun : null;

        return boundCall.Fun;
    }
    
    private ExprSyntax? BindableSyntaxFor(ExprSyntax syntax)
    {
        return syntax.Parent switch
        {
            MemberAccessExprSyntax memberAccess when memberAccess.Member == syntax
                => memberAccess,
            SubscriptExprSyntax subscript when subscript.Left == syntax
                => subscript,

            //TODO: Handle argument names through BoundCall / whatnot
            ArgSyntax arg when arg.Name == syntax 
                => null, 
            
            // Annotation don't resolve to symbols. We can early exit them.
            AnnotationSyntax annotation when annotation.Name == syntax
                => null,

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