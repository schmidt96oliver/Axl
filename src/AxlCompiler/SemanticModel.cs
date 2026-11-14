using System.Collections.Immutable;
using System.Diagnostics;
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
    
    
    public BoundNode BoundNodeFor(ExprSyntax syntax) 
        => TryBoundNodeFor(syntax) ?? new BoundError(compilation.BaseLibrary.Error, syntax);

    private BoundNode? TryBoundNodeFor(ExprSyntax syntax)
    {
        //TODO: Cache the results
        var sideEffects = new BinderSideEffects();
        CompleteEnclosingSymbol(syntax.Location, compilation, sideEffects);

        if (sideEffects.BoundNodes.TryGetValue(syntax, out var boundNodes))
            return boundNodes[^1]; //TODO: Which one to pick?

        return null;
    }

    
    public SymbolReference SymbolFor(Token idToken)
    {
        if (idToken.Kind is not TokenKind.Identifier || idToken.Text is "")
            return new SymbolReference.Unresolved([], UnresolvedSymbolReason.NotFound, idToken.Location);

        if (idToken.Parent is NameRefSyntax nameRef)
        {
            return TryReferencedSymbolFor(nameRef) ??
                   new SymbolReference.Unresolved([], UnresolvedSymbolReason.NotFound, idToken.Location);
        }

        return TryDeclaredSymbolFor(idToken) is { } symbol
            ? new SymbolReference.Resolved(symbol, idToken.Location)
            : new SymbolReference.Unresolved([], UnresolvedSymbolReason.NotFound, idToken.Location);
    }

    /// <summary>
    /// Returns the referenced symbol, but <c>null</c> if it
    /// could not be resolved due to an internal error. Used for
    /// testing.
    /// </summary>
    internal SymbolReference? TryReferencedSymbolFor(NameRefSyntax syntax)
    {
        if (syntax.Parent is AnnotationSyntax)
            return new SymbolReference.Unresolved([], UnresolvedSymbolReason.NotFound, syntax.Location);

        if (syntax.Parent is ArgSyntax arg && arg.Name == syntax)
            return ReferencedSymbolForNamedArgument(arg);

        ExprSyntax bindableSyntax = syntax.Parent is MemberAccessExprSyntax memberAccess && memberAccess.Member == syntax
            ? memberAccess
            : syntax;

        var boundNode = TryBoundNodeFor(bindableSyntax);
        if (boundNode is BoundTypeGroup or BoundFunGroup && 
            bindableSyntax.Parent is SubscriptExprSyntax subscript &&
            subscript.Left == bindableSyntax)
        {
            bindableSyntax = subscript;
            boundNode = TryBoundNodeFor(subscript);
        }

        return boundNode switch
        {
            IBoundSymbolRef symbolRef => new SymbolReference.Resolved(symbolRef.Symbol, syntax.Location),
            BoundTypeGroup typeGroup => ReferencedSymbolFor(typeGroup, bindableSyntax, syntax),
            BoundFunGroup funGroup => ReferencedSymbolFor(funGroup, bindableSyntax, syntax),
            BoundLookupError lookupError => new SymbolReference.Unresolved(lookupError.Candidates, lookupError.Reason,
                syntax.Location),
            _ => null
        };
    }

    private SymbolReference ReferencedSymbolFor(BoundTypeGroup typeGroup, ExprSyntax syntax, NameRefSyntax nameSyntax)
    {
        if (syntax.Parent is CallExprSyntax callExprSyntax && callExprSyntax.Callee == syntax)
        {
            var boundCall = BoundNodeFor(callExprSyntax);
            if (boundCall is BoundStructInit structInit)
                return new SymbolReference.Resolved(structInit.Type, nameSyntax.Location);
            if (boundCall is BoundStructInitError structInitError)
            {
                // If there is only a single candidate that has its type
                // parameters filled, it resolves correctly. Otherwise,
                // it's unresolved (too many types or failed inference).
                if (structInitError.Candidates is [{ HasFilledTypeParameters: true } singleType])
                    return new SymbolReference.Resolved(singleType, nameSyntax.Location);

                return new SymbolReference.Unresolved(
                    structInitError.Candidates.CastArray<Symbol>(),
                    UnresolvedSymbolReason.NoApplicableType, nameSyntax.Location);
            }
        }

        // Same as above
        if (typeGroup.Candidates is [{ HasFilledTypeParameters: true } single])
            return new SymbolReference.Resolved(single, nameSyntax.Location);
        
        return new SymbolReference.Unresolved(
            typeGroup.Candidates.CastArray<Symbol>(),
            UnresolvedSymbolReason.Ambiguous, nameSyntax.Location);
    }

    private SymbolReference ReferencedSymbolFor(BoundFunGroup funGroup, ExprSyntax syntax, NameRefSyntax nameSyntax)
    {
        var callSyntax = syntax.Ancestors().OfType<CallExprSyntax>().FirstOrDefault();
        if (callSyntax is null || callSyntax.Callee != syntax)
            return new SymbolReference.Unresolved(funGroup.Candidates.CastArray<Symbol>(), UnresolvedSymbolReason.NotUsable,
                nameSyntax.Location);

        if (BoundNodeFor(callSyntax) is not BoundCall boundCall)
        {
            // The fun group is not part of a call or it had an error.
            return new SymbolReference.Unresolved(funGroup.Candidates.CastArray<Symbol>(),
                UnresolvedSymbolReason.NoApplicableOverload, nameSyntax.Location);
        }

        return new SymbolReference.Resolved(boundCall.Fun, nameSyntax.Location);
    }

    private SymbolReference? ReferencedSymbolForNamedArgument(ArgSyntax syntax)
    {
        // Arg must be part of ArgList, which must be part of CallExpr.
        
        Debug.Assert(syntax.Parent is ArgListSyntax argList && argList.Arguments.Contains(syntax));
        Debug.Assert(syntax.Parent.Parent is CallExprSyntax);
        
        var callSyntax = (CallExprSyntax)syntax.Parent.Parent;
        Debug.Assert(callSyntax.ArgList == syntax.Parent);

        var bound = TryBoundNodeFor(callSyntax);
        if (bound is null) return null;
        if (bound is not BoundStructInit structInit)
            return new SymbolReference.Unresolved([], UnresolvedSymbolReason.NotFound, syntax.Name!.Location);

        Debug.Assert(syntax.Name is not null);
        var name = syntax.Name.Text.ToString();
        var fieldInit = structInit.FieldInits.First(init => init.Field.Name == name);
        return new SymbolReference.Resolved(fieldInit.Field, syntax.Name!.Location);
    }

    internal Symbol? TryDeclaredSymbolFor(Token token)
    {
        if (token.Kind is not TokenKind.Identifier || token.Parent is NameRefSyntax)
            return null;

        var symbol = compilation.Scripts.Concat(compilation.RootNamespace.Members)
            .FirstOrDefault(s => s.DeclarationContains(token.Location));
        while (symbol is not null)
        {
            Debug.Assert(symbol.DeclarationContains(token.Location));

            if (symbol.NameSyntax.Contains(token))
                return symbol;

            switch (symbol)
            {
                // Structs have type parameters
                case StructSymbol structSymbol when structSymbol.DeclarationSyntax
                    .TypeParameterList?.Location.Contains(token.Location) == true:
                {
                    if (token.Parent is not TypeParamSyntax) return null;
                    
                    return structSymbol.TypeParameters.FirstOrDefault(typeParam =>
                        typeParam.NameSyntax.Contains(token));
                }

                // Scripts have members and a script fun. Note that the script fun
                // will span the entire script.
                case ScriptSymbol script:
                {
                    symbol = script.Members.FirstOrDefault(member => member.DeclarationContains(token.Location)) ??
                             script.Fun;
                    break;
                }

                // SourceFuns have type parameters and normal parameters
                case SourceFunSymbol sourceFun:
                {
                    if (sourceFun.DeclarationSyntax.TypeParameterList?.Location.Contains(token.Location) == true)
                    {
                        if (token.Parent is not TypeParamSyntax) return null;
                        return sourceFun.TypeParameters.FirstOrDefault(typeParam =>
                            typeParam.NameSyntax.Contains(token));
                    }

                    if (sourceFun.DeclarationSyntax.ParameterList.Location.Contains(token.Location))
                    {
                        if (token.Parent is not ParamSyntax) return null;
                        return sourceFun.Parameters.FirstOrDefault(param => param.NameSyntax.Contains(token));
                    }

                    if (token.Parent is VarDeclSyntax varDecl)
                        return TryDeclaredLocalInFunBody(sourceFun, varDecl);

                    return null;
                }
                    
                // Script funs only have a body
                case ScriptFunSymbol scriptFun:
                {
                    if (token.Parent is VarDeclSyntax varDecl)
                        return TryDeclaredLocalInFunBody(scriptFun, varDecl);
                    return null;
                }
                    
                // Anything else (namespace e.g.), we can just search their members.
                default:
                    symbol = symbol.Members.FirstOrDefault(member => member.DeclarationContains(token.Location));
                    break;
            }
        }
        
        return null;
    }

    private static VariableSymbol? TryDeclaredLocalInFunBody(FunSymbol fun, VarDeclSyntax varDeclSyntax)
    {
        var sideEffects = new BinderSideEffects();
        fun.Complete(sideEffects);
        if (!sideEffects.BoundNodes.TryGetValue(varDeclSyntax, out var boundStmts) || boundStmts.Count == 0)
            return null;

        return (boundStmts[^1] as BoundVarDecl)?.Variable;
    }


    public ImmutableArray<SymbolReference> FindReferences(Symbol symbol, SyntaxTree scope)
    {
        symbol = symbol.OriginalDefinition;
        
        // Narrow scope if possible
        var scopeLocation = symbol switch
        {
            LocalSymbol local => local.Owner.DeclaringSyntax.FirstOrDefault(s => s.Tree == scope)?.Location,

            _ => new SourceLocation(scope.SourceText, scope.SourceText.Range)
        };
        if (scopeLocation is null)
            return [];

        return
        [
            .. scope.Tokens.Where(t =>
                    scopeLocation.Value.Contains(t.Location) && t.Kind is TokenKind.Identifier &&
                    t.Text.SequenceEqual(symbol.Name))
                .Select(SymbolFor)
                .Where(s => s.Symbols.Any(inner => Equals(inner.OriginalDefinition, symbol)))
        ];
    }
    

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
        CompleteEnclosingSymbol(probeToken.Location, compilationWithProbe, probe);

        _probes[(tree, position)] = probe;
        return probe;
    }
    
    private static void CompleteEnclosingSymbol(SourceLocation location, Compilation compilation, BinderSideEffects sideEffects)
    {
        var symbol = compilation.Scripts.Concat(compilation.RootNamespace.Members)
            .FirstOrDefault(s => s.DeclarationContains(location));

        while (symbol is not null)
        {
            var next = symbol.Members.FirstOrDefault(member => member.DeclarationContains(location));
            if (next is null)
            {
                symbol.Complete(sideEffects);
                return;
            }

            symbol = next;
        }
        
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