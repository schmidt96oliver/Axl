using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Binding.Scopes;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Binding;

public enum TypeUsage
{
    /// <summary>
    /// The type will not be visible outside the current owner.
    /// </summary>
    Local,
    
    /// <summary>
    /// The type will be visible outside the current owner, so
    /// the type must at least be as visible as owner.
    /// </summary>
    Signature
}

[Flags]
public enum ValueUsage
{
    /// <summary>
    /// The value will not be used.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Value will be read as a value.
    /// </summary>
    Read = 1 << 1,

    /// <summary>
    /// Value will be assigned to.
    /// </summary>
    Assign = 1 << 2,

    /// <summary>
    /// Value will be passed through "mut" to a mut fun parameter.
    /// </summary>
    PassAsMut = 1 << 3,

    /// <summary>
    /// Value will be returned from a mut-returning fun.
    /// </summary>
    ReturnAsMut = 1 << 4,
}

public sealed class Binder(Scope scope, BinderSideEffects sideEffects)
{
    public Scope Scope { get; } = scope;
    public BinderSideEffects SideEffects { get; } = sideEffects;

    #region Comfort Stuff

    private TypeSymbol Bool => Scope.Compilation.BaseLibrary.Bool;
    
    private TypeSymbol String => Scope.Compilation.BaseLibrary.String;
    
    private TypeSymbol Unit => Scope.Compilation.BaseLibrary.Unit;

    private ErrorTypeSymbol ErrorType => Scope.Compilation.BaseLibrary.Error;
    
    private TypeSymbol NeverType => Scope.Compilation.BaseLibrary.Never;
    
    
    public Binder WithSideEffects(BinderSideEffects sideEffects)
        => new(Scope, sideEffects);

    public Binder WithScope(Scope scope)
        => new(scope, SideEffects);
    
    public Binder EnterFunSignature(FunSymbol fun)
        => WithScope(Scope.WithOwner(fun));

    public Binder EnterFunBody()
    {
        Guard.IsState(Scope.Owner is FunSymbol);
        return WithScope(new FunBodyScope(Scope));
    }

    public Binder EnterBlock()
        => WithScope(new BlockScope(Scope));

    public Binder WithAdditionalFlags(BindingFlags flags)
        => WithScope(new FlagsScope(Scope, Scope.Flags | flags));
    
    #endregion


    private bool IsAssignableTo(TypeSymbol source, TypeSymbol target)
    {
        // Errors are silent
        if (source is ErrorTypeSymbol || target is ErrorTypeSymbol) return true;
        
        // Never assigns to anything
        if (source == NeverType) return true;

        return source == target;
    }

    private bool CheckType(BoundValue value, TypeSymbol expected)
    {
        if (!IsAssignableTo(value.Type, expected))
        {
            SideEffects.ReportError(new Diagnostic.TypeMismatch(
                Value: value,
                Expected: expected));
            return false;
        }

        return true;
    }
    
    private bool CheckAccessibility(Symbol symbol, SyntaxNode syntax)
    {
        if (!Scope.IsAccessible(symbol))
        {
            SideEffects.ReportError(new Diagnostic.NotAccessible([symbol], syntax));
            return false;
        }

        return true;
    }


    private BoundError BindError(SyntaxNode syntax, Diagnostic.Error? error)
    {
        SideEffects.ReportError(error);
        return new BoundError(ErrorType, syntax);
    }

    
    #region Lookup

    private enum LookupOptions
    {
        Any,
        OnlyType,
        OnlyNamespace,
        MustBeCallable,
    }
    
    private LookupViability GetViability(Symbol symbol, LookupOptions options)
    {
        if (!Scope.IsAccessible(symbol)) 
            return LookupViability.NotAccessible;

        if (options is LookupOptions.OnlyType && symbol is not TypeSymbol)
            return LookupViability.WrongKind;
        if (options is LookupOptions.MustBeCallable && symbol is not (TypeSymbol or FunSymbol))
            return LookupViability.WrongKind;
        if (options is LookupOptions.OnlyNamespace && symbol is not NamespaceSymbol)
            return LookupViability.WrongKind;

        return LookupViability.Viable;
    }

    private LookupResult LookupName(string name, LookupOptions options)
    {
        var result = new LookupResult();

        for (var scope = Scope; scope != null; scope = scope.EnclosingScope)
        {
            var here = new LookupResult();
            foreach (var s in scope.Lookup(name))
                here.MergeSingle(s, GetViability(s, options));

            result.MergeWith(here);

            if (result.Viability == LookupViability.Viable)
                break;
        }

        return result;
    }

    private LookupResult LookupMembers(NamespaceOrTypeSymbol parent, string name)
    {
        var result = new LookupResult();

        foreach (var s in parent.LookupMembers(name))
            result.MergeSingle(s, GetViability(s, LookupOptions.Any));

        return result;
    }
    
    #endregion
    
    
    #region Namespaces and Scripts

    public static NamespaceSymbol BindRootNamespace(Compilation compilation)
    {
        var decl = GetRootNamespaceDeclaration([.. compilation.SyntaxTrees.Where(IsNamespaceFile)]);
        return new SourceNamespaceSymbol(compilation, decl, owner: null, compilation.GlobalScope, isRoot: true);
    }

    public static ImmutableArray<ScriptSymbol> BindScripts(Compilation compilation) =>
    [
        .. compilation.SyntaxTrees.Where(tree => !IsNamespaceFile(tree))
            .Select(tree => new ScriptSymbol(tree, compilation.GlobalScope))
    ];

    private static NamespaceDeclaration GetRootNamespaceDeclaration(ImmutableArray<SyntaxTree> trees)
    {
        var rootNamespaceDeclaration = new NamespaceDeclarationBuilder("");

        foreach (var tree in trees)
        {
            if (tree.FileSyntax.SyntaxNodes().FirstOrDefault() is not NamespaceDeclSyntax syntax)
                throw new UnreachableException();

            // Namespace names have multiple parts "A.B.C" and each parts needs
            // to declare a new namespace, even if it doesn't have any syntax.
            
            var decl = rootNamespaceDeclaration;
            foreach (var namePart in syntax.Name.Parts.Select(t => t.Text.ToString()))
            {
                // For an empty name, always insert a new namespace,
                // don't group them together.
                
                var child = decl.TryGetChild(namePart);
                if (child is null || namePart is "")
                    child = decl.AddChild(namePart);

                decl = child;
            }
            
            decl.AddSyntaxTree(tree);
        }

        return rootNamespaceDeclaration.ToNamespaceDeclaration();
    }

    private static bool IsNamespaceFile(SyntaxTree tree)
        => tree.FileSyntax.SyntaxNodes().FirstOrDefault() is NamespaceDeclSyntax;

    
    public BoundBlock BindScriptBody(FileSyntax syntax)
    {
        var syntaxNodes = syntax.SyntaxNodes().ToImmutableArray();
        
        // Skip usings at the start of the file
        var index = 0;
        for (; index < syntaxNodes.Length && syntaxNodes[index] is UsingDirectiveSyntax; index++)
        {
        }

        var inBlockBinder = EnterBlock();

        var stmts = inBlockBinder.BindStmts(syntaxNodes[index..]);
        return new BoundBlock(stmts, Unit, syntax);
    }
    
    public ImmutableArray<Symbol> BindScriptMembers(FileSyntax syntax, out LazyUsingsScope usingsScope)
    {
        var syntaxNodes = syntax.SyntaxNodes().ToImmutableArray();
        
        // Usings
        var index = 0;
        var usingSyntaxes = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
        for (; index < syntaxNodes.Length && syntaxNodes[index] is UsingDirectiveSyntax usingSyntax; index++)
            usingSyntaxes.Add(usingSyntax);

        // The scope can immediately be completed, since
        // we are not currently binding a namespace.
        usingsScope = new LazyUsingsScope(Scope, usingSyntaxes.DrainToImmutable());
        var usingsBinder = WithScope(usingsScope);

        return
        [
            .. syntax.Children.OfType<StructDeclSyntax>().Select(usingsBinder.BindStructDeclaration),
            .. syntax.Children.OfType<FunDeclSyntax>().Select(usingsBinder.BindSourceFunDeclaration)
        ];
    }

    public NamespaceSymbol BindNamespaceDeclaration(NamespaceDeclaration declaration)
    {
        if (Scope.Owner.Owner is null &&
            Scope.Compilation.ReferencedCompilations.Any(c =>
                c.RootNamespace.LookupMembers(declaration.Name).Length > 0))
        {
            // The namespace is being declared on global or root namespace
            // but is already declared in a referenced compilation.
            SideEffects.ReportError(new Diagnostic.NamespaceAlreadyDeclared(declaration));
            
            // If base namespace is declared again in spite of an error, that
            // would lead to the Compilation finding ambigous references to
            // it, which create a massive amount of followup errors. Thus,
            // empty its name.
            if (declaration.Name is Names.BaseNamespace)
            {
                var emptyNameDeclaration = new NamespaceDeclaration("", declaration.Children, declaration.SyntaxTrees);
                return new SourceNamespaceSymbol(Scope.Compilation, emptyNameDeclaration, Scope.Owner, Scope, isRoot: false);
            }
        }

        return new SourceNamespaceSymbol(Scope.Compilation, declaration, Scope.Owner, Scope, isRoot: false);
    }
    
    public ImmutableArray<Symbol> BindNamespaceFile(SyntaxTree syntax, out LazyUsingsScope usingsScope)
    {
        // In a namespace file, the first declaration must be the namespace
        // syntax.
        var syntaxNodes = syntax.FileSyntax.SyntaxNodes().ToImmutableArray();
        Debug.Assert(syntaxNodes[0] is NamespaceDeclSyntax);

        // Usings
        var index = 1;
        var usingSyntaxes = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();
        for (; index < syntaxNodes.Length && syntaxNodes[index] is UsingDirectiveSyntax usingSyntax; index++)
            usingSyntaxes.Add(usingSyntax);
        
        // The scope is lazy and will be completed by the caller.
        // We cannot complete it here, because it might look up inside this namespace's members.
        usingsScope = new LazyUsingsScope(Scope, usingSyntaxes.DrainToImmutable());
        var usingsBinder = WithScope(usingsScope);
        
        // Declarations
        var symbols = ImmutableArray.CreateBuilder<Symbol>();
        for (; index < syntaxNodes.Length; index++)
        {
            switch (syntaxNodes[index])
            {
                case FunDeclSyntax funDecl:
                    symbols.Add(usingsBinder.BindSourceFunDeclaration(funDecl));
                    break;

                case StructDeclSyntax structDecl:
                    symbols.Add(usingsBinder.BindStructDeclaration(structDecl));
                    break;

                case NamespaceDeclSyntax namespaceDecl:
                    // Multiple namespace declaration
                    SideEffects.ReportError(new Diagnostic.DuplicateNamespaceDeclaration(namespaceDecl));
                    break;

                case StmtSyntax stmtSyntax:
                    SideEffects.ReportError(new Diagnostic.StmtInNamespaceFile(stmtSyntax));
                    break;

                case UsingDirectiveSyntax usingDirective:
                    // All valid using directives have been handled,
                    // so this one is invalid.
                    SideEffects.ReportError(new Diagnostic.UsingDirectiveAfterCode(usingDirective));
                    break;

                default:
                    SideEffects.ReportError(new Diagnostic.UnsupportedFeature(syntaxNodes[index]));
                    break;
            }
        }

        return symbols.DrainToImmutable();
    }

    private NamespaceSymbol? BindUsingDirective(UsingDirectiveSyntax syntax)
    {
        var boundExpr = BindExpr(syntax.NameExpr, LookupOptions.OnlyNamespace);
        if (boundExpr is not BoundNamespaceRef nsRef)
        {
            if (boundExpr is not BoundError)
                SideEffects.ReportError(new Diagnostic.NotANamespace(boundExpr));
            return null;
        }

        return nsRef.Namespace;
    }

    public ImmutableArray<NamespaceSymbol> BindUsingDirectives(ImmutableArray<UsingDirectiveSyntax> syntaxes)
    {
        var namespaces = ImmutableArray.CreateBuilder<NamespaceSymbol>();
        foreach (var syntax in syntaxes)
        {
            var bound = BindUsingDirective(syntax);
            if (bound is null) continue;
            if (namespaces.Contains(bound) ||
                Scope.Owner?.SelfAndOwners().Contains(bound) == true ||
                bound == Scope.Compilation.BaseLibrary.BaseNamespace)
            {
                SideEffects.ReportLint(new Diagnostic.UnnecessaryUsing(syntax));
                continue;
            }

            namespaces.Add(bound);
        }

        return namespaces.DrainToImmutable();
    }
    
    
    #endregion
    
    #region Declarations
    
    public SourceFunSymbol BindSourceFunDeclaration(FunDeclSyntax syntax)
    {
        Debug.Assert(Scope.OwnerAsNamespaceOrType is not null);

        // Primitive annotation
        var annotations = syntax.Annotations.Select(BindAnnotation);
        BoundAnnotation? intrinsicAnnotation = null;
        foreach (var annotation in annotations)
        {
            if (annotation is null) continue;
            if (annotation.Kind is AnnotationKind.Intrinsic)
                intrinsicAnnotation = annotation;
            else
                SideEffects.ReportError(new Diagnostic.InvalidFunAnnotation(annotation));
        }

        // ---
        var name = syntax.Name.Text.ToString();
        
        var isPublic = syntax.PubKw is not null;
        var receiverType = syntax.StaticKw is not null ? null : Scope.OwnerAsType;
        var isMutatingReceiver = syntax.MutKw is not null;

        if (receiverType is null && isMutatingReceiver)
            SideEffects.ReportError(new Diagnostic.StaticFunCannotBeMut(syntax.MutKw!, name));

        var funSymbol = new SourceFunSymbol(syntax,
            Scope,
            isPublic,
            intrinsicAnnotation);

        SideEffects.AddResolvedSymbol(syntax.Name.Location, funSymbol);
        return funSymbol;
    }

    public StructSymbol BindStructDeclaration(StructDeclSyntax syntax)
    {
        // Primitive annotation
        var annotations = syntax.Annotations.Select(BindAnnotation);
        PrimitiveKind? primitiveKind = null;
        foreach (var annotation in annotations)
        {
            if (annotation is null) continue;
            if (annotation.Kind is AnnotationKind.Primitive)
            {
                if (primitiveKind is null && syntax.Name.Text is not "")
                {
                    if (Enum.TryParse(syntax.Name.Text, out PrimitiveKind kind))
                        primitiveKind = kind;
                    else
                    {
                        SideEffects.ReportError(new Diagnostic.UnknownPrimitive(annotation,
                            syntax.Name.Text.ToString()));
                    }
                }
            }
            else if (annotation.Kind is not AnnotationKind.Primitive)
                SideEffects.ReportError(new Diagnostic.InvalidStructAnnotation(annotation));
        }

        var isPublic = syntax.PubKw is not null;
        if (syntax.PubKw is not null && Scope.Owner is ScriptSymbol)
        {
            SideEffects.ReportError(new Diagnostic.InvalidScriptStructModifier(syntax.PubKw));
            isPublic = false;
        }

        var symbol = new StructSymbol(syntax, enclosingScope: Scope, isPublic, primitiveKind);
        if (symbol.Name.Length > 0)
            SideEffects.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }

    public FieldSymbol BindFieldDeclaration(FieldDeclSyntax syntax)
    {
        if (Scope.Owner is not StructSymbol structSymbol)
            throw new InvalidOperationException("Can only bind fields inside structs.");

        if (structSymbol.IsPrimitive)
            SideEffects.ReportError(new Diagnostic.FieldDeclarationOnPrimitive(syntax, structSymbol));
        
        var symbol = new FieldDefinitionSymbol(syntax, Scope, structSymbol);
        if (symbol.Name.Length > 0)
            SideEffects.AddResolvedSymbol(syntax.Name.Location, symbol);

        return symbol;
    }

    

    public BoundAnnotation? BindAnnotation(AnnotationSyntax syntax)
    {
        if (syntax.Name.Text is "")
            return null;

        if (Enum.TryParse(syntax.Name.Text, out AnnotationKind kind))
            return new BoundAnnotation(kind, syntax);
        
        SideEffects.ReportError(new Diagnostic.UnknownAnnotation(syntax));
        return null;
    }
    
    
    public void CheckDuplicateDeclarations(ImmutableArray<Symbol> declared)
    {
        var symbolsByName = declared
            .Where(symbol => symbol.Name is not "")
            .GroupBy(symbol => symbol.Name)
            .Select(duplicates => duplicates.ToImmutableArray())
            .ToImmutableArray();

        foreach (var duplicateArray in symbolsByName.Where(duplicates => duplicates.Length > 1))
        {
            if (duplicateArray.All(s => s is FunDefinitionSymbol))
            {
                // We need to check their signatures
                CheckDuplicateDeclarationsInFunGroup([.. duplicateArray.Select(s => (FunDefinitionSymbol)s)]);
            }

            else if (duplicateArray.All(s => s is TypeSymbol and not TypeParameterSymbol))
            {
                // We need to check type parameters
                CheckDuplicateDeclarationsInTypeGroup([.. duplicateArray.Cast<TypeSymbol>()]);
            }
            
            // Anything mixed-type is an error
            else 
            {
                SideEffects.ReportError(new Diagnostic.AlreadyDeclared(duplicateArray));
            }
        }
    }

    private void CheckDuplicateDeclarationsInFunGroup(IEnumerable<FunDefinitionSymbol> funsWithSameName)
    {
        var remaining = funsWithSameName.ToList();
        while (remaining.Count > 0)
        {
            var first = remaining[0];
            var sameSignature = remaining
                .Where(fun => SignatureEqual(fun, first))
                .ToImmutableArray();
            if (sameSignature.Length > 1)
            {
                // Errors in the signature should not report another error.
                var duplicatesToReport = sameSignature
                    .Where(fun => fun.Parameters.All(param => param.Type is not ErrorTypeSymbol))
                    .ToImmutableArray();

                if (duplicatesToReport.Length > 1)
                    SideEffects.ReportError(new Diagnostic.DuplicateFunDeclarations(sameSignature));
            }

            remaining.RemoveAll(sameSignature.Contains);
        }

        bool SignatureEqual(FunSymbol a, FunSymbol b)
        {
            if (a.TypeParameters.Length != b.TypeParameters.Length) return false;
            if (a.Parameters.Length != b.Parameters.Length) return false;
            
            for (var i = 0; i < a.Parameters.Length; i++)
            {
                if (!AreParamsEqual(a.Parameters[i], b.Parameters[i]))
                    return false;
            }

            return true;
        }

        bool AreParamsEqual(ParameterSymbol a, ParameterSymbol b)
        {
            // Type parameters compare by ordinal if they belong to
            // the funs signature (i.e. their owner is the same as the
            // parameters owner), because they could have different names.
            
            if (a.IsMut != b.IsMut) 
                return false;
            
            if (a.Type is TypeParameterSymbol aParam && Equals(aParam.Owner, a.Owner) &&
                b.Type is TypeParameterSymbol bParam && Equals(bParam.Owner, b.Owner))
            {
                return aParam.Ordinal == bParam.Ordinal;
            }

            return a.Type == b.Type;
        }
    }

    private void CheckDuplicateDeclarationsInTypeGroup(IEnumerable<TypeSymbol> typesWithSameName)
    {
        // Group based on type parameter arity
        var duplicatesWithSameArity = typesWithSameName
            .GroupBy(type => type.TypeParameters.Length)
            .Select(duplicates => duplicates.ToImmutableArray())
            .Where(duplicates => duplicates.Length > 1)
            .ToImmutableArray();

        foreach (var duplicateGroup in duplicatesWithSameArity)
        {
            SideEffects.ReportError(new Diagnostic.DuplicateTypeDeclarations(duplicateGroup));
        }
    }

    public void CheckRecursiveStructLayout(StructSymbol symbol)
    {
        Recurse(symbol, []);
        return;

        void Recurse(StructSymbol current, FieldDefinitionSymbol[] path)
        {
            foreach (var field in current.Members.OfType<FieldSymbol>().Select(f => f.OriginalDefinition))
            {
                if (field.Type is not StructSymbol fieldStructType)
                    continue;

                if (fieldStructType == symbol)
                {
                    SideEffects.ReportError(new Diagnostic.RecursiveStructLayout(symbol, [.. path, field]));
                }
                else
                    Recurse(fieldStructType, [.. path, field]);
            }
        }
    }
    
    #endregion
    
    #region Fun Signature/Body

    public BoundBlock BindSourceFunBody(FunBodySyntax syntax)
    {
        if (Scope.OwnerAsFun is not SourceFunSymbol fun)
            throw new InvalidOperationException($"{nameof(BindSourceFunBody)} only valid on funs.");

        var bodyBinder = EnterFunBody();

        // --- Empty body
        if (syntax.Expr is null)
        {
            SideEffects.ReportError(new Diagnostic.MissingFunBody(fun, fun.DeclarationSyntax));
            return new BoundBlock([], Unit);
        }

        // --- Expression body
        if (syntax.IsExpressionBodied)
        {
            var value = bodyBinder.BindReturnValue(syntax.Expr, out var isMutReturn);
            return new BoundBlock([new BoundReturn(value, isMutReturn, NeverType)], Unit);
        }

        // --- Block body
        Debug.Assert(syntax.Expr is BlockExprSyntax);
        return bodyBinder.BindBlock((BlockExprSyntax)syntax.Expr);
    }

    public FunBody BindIntrinsicFunBody(FunBodySyntax syntax, BoundAnnotation annotation)
    {
        if (Scope.OwnerAsFun is not FunSymbol fun)
            throw new InvalidOperationException($"{nameof(BindSourceFunBody)} only valid on funs.");

        if (syntax.IsExpressionBodied || syntax.Expr is not null)
            SideEffects.DiagnosticBag.ReportError(new Diagnostic.IntrinsicFunWithBody(fun, syntax));
        
        if (fun.SelfAndParameters.Any(p => !p.Type.IsPrimitive) || 
            fun.ReturnType != Unit && !fun.ReturnType.IsPrimitive)
        {
            SideEffects.DiagnosticBag.ReportError(new Diagnostic.UnknownIntrinsic(annotation));
            return new BoundBlock([], Unit);
        }

        var parameters = fun.SelfAndParameters.Select(p => p.Type.PrimitiveKind!.Value).ToImmutableArray();
        var returnType = fun.ReturnType == Unit ? null : fun.ReturnType.PrimitiveKind;

        if (IntrinsicLookup.TryGetIntrinsic(fun.Name, parameters, returnType) is not Intrinsic intrinsic)
        {
            SideEffects.DiagnosticBag.ReportError(new Diagnostic.UnknownIntrinsic(annotation));
            return new BoundBlock([], Unit);
        }

        return intrinsic;
    }
    
    public (TypeSymbol Type, bool IsMut) BindFunReturnType(ReturnTypeAnnotationSyntax? returnTypeAnnotation)
    {
        return returnTypeAnnotation is not null
            ? (BindType(returnTypeAnnotation.TypeExpr, TypeUsage.Signature).Type, returnTypeAnnotation.MutKw is not null)
            : (Unit, false);
    }
    
    public ImmutableArray<ParameterSymbol> BindParameters(ParamListSyntax syntax)
    {
        var parameters = syntax.Parameters
            .Select(BindParameter)
            .ToImmutableArray();

        // Report duplicate parameter errors only on parameters
        // with a non-empty name.
        var duplicateParamGroups = parameters
            .Where(param => param.Name is not "")
            .GroupBy(param => param.Name)
            .Select(grp => grp.ToImmutableArray())
            .Where(grp => grp.Length > 1);
        foreach (var duplicateGroup in duplicateParamGroups)
            SideEffects.ReportError(new Diagnostic.DuplicateParameters(duplicateGroup));

        return parameters;
    }

    private ParameterSymbol BindParameter(ParamSyntax syntax)
    {
        if (Scope.OwnerAsFun is null)
            throw new InvalidOperationException($"{nameof(BindParameter)} only valid on funs.");

        var type = BindType(syntax.TypeAnnotation, TypeUsage.Signature).Type;
        var symbol = new ParameterSymbol(syntax.Name.Identifier, 
            Scope.OwnerAsFun!, 
            type,
            isMut: syntax.MutKw is not null, 
            isSelf: false, 
            originalDefinition: null,
            syntax);
        SideEffects.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }

    public ImmutableArray<TypeParameterSymbol> BindTypeParameters(TypeParamListSyntax syntax)
    {
        var typeParameters = syntax.TypeParameters
            .Select(BindTypeParameter)
            .ToImmutableArray();

        // Note: Duplication will be reported by Member duplication
        //       check.
        return typeParameters;
    }

    private TypeParameterSymbol BindTypeParameter(TypeParamSyntax syntax, int ordinal)
    {
        var symbol = new TypeParameterSymbol(syntax.Name.Text.ToString(), Scope.Owner, ordinal, syntax);

        // Type Parameter must not
        // (a) have the same name as its owner
        // (b) hide type parameter of outer type
        // (c) hide a visible type (lint)
        // Only one of those should be reported.
        if (symbol.Name == Scope.Owner.Name)
        {
            SideEffects.ReportError(new Diagnostic.TypeParameterHasOwnerName(symbol, Scope.Owner));
        }
        else if (symbol.Name is not "" &&
                 Scope.Owner.Owners()
                     .SelectMany(s => s switch
                     {
                         TypeSymbol type => type.TypeParameters.Where(t => t.Name == symbol.Name),
                         FunSymbol fun => fun.TypeParameters.Where(t => t.Name == symbol.Name),
                         _ => []
                     })
                     .FirstOrDefault() is { } hiddenTypeParameter)
        {
            SideEffects.ReportError(new Diagnostic.TypeParametersHideOuterTypeParameter(syntax, hiddenTypeParameter));
        }
        else if (symbol.Name is not "" && LookupName(symbol.Name, LookupOptions.OnlyType) is
                     { Viability: LookupViability.Viable, Symbols: [TypeSymbol hiddenType, ..] })
        {
            SideEffects.ReportLint(new Diagnostic.TypeParameterHidesType(symbol, hiddenType));
        }

        SideEffects.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }

    public void CheckOperatorSignature(SourceFunSymbol funSymbol)
    {
        if (funSymbol.Owner is not TypeSymbol)
        {
            SideEffects.ReportError(new Diagnostic.OperatorOutsideType(funSymbol.DeclarationSyntax.Name));
            return;
        }

        if (funSymbol.SelfParameter is null)
        {
            SideEffects.ReportError(new Diagnostic.OperatorCannotBeStatic(funSymbol.Name,
                funSymbol.DeclarationSyntax.StaticKw!));
            return;
        }

        if (funSymbol.SelfParameter?.IsMut == true)
        {
            SideEffects.ReportError(
                new Diagnostic.OperatorCannotBeMut(funSymbol.Name, funSymbol.DeclarationSyntax.MutKw!));
            return;
        }


        var opToken = funSymbol.DeclarationSyntax.Name.Token;
        var isUnary = SyntaxFacts.IsUnaryOperator(opToken.Kind);
        var isBinary = SyntaxFacts.IsBinaryOperator(opToken.Kind);
        Debug.Assert(isUnary || isBinary);

        // Declared operator must match unary/binary
        var isParamCountValid = (isUnary, isBinary, funSymbol.Parameters.Length) is
            (true, true, 0 or 1) or (true, false, 0) or (false, true, 1);

        if (!isParamCountValid)
            SideEffects.ReportError(new Diagnostic.WrongOperatorParameterCount(funSymbol));

        // Comparison operators must return bool
        if (SyntaxFacts.IsComparisonOperator(opToken.Kind) &&
            funSymbol.ReturnType != Bool && funSymbol.ReturnType is not ErrorTypeSymbol)
        {
            SideEffects.ReportError(new Diagnostic.WrongOperatorReturnType(funSymbol, Bool));
        }
    }

    #endregion
    
    #region Stmts
    
    private BoundStmt BindStmt(StmtSyntax syntax) => syntax switch
    {
        VarDeclSyntax varDeclSyntax => BindVarDecl(varDeclSyntax),
        ExprStmtSyntax exprStmt => BindExprStmt(exprStmt.Expr),
        WhileStmtSyntax whileStmtSyntax => BindWhile(whileStmtSyntax),
    };

    private BoundStmt BindExprStmt(ExprSyntax exprSyntax)
    {
        if (exprSyntax is IfExprSyntax ifExprSyntax)
            return BindIfStmt(ifExprSyntax);

        return BindValue(exprSyntax, ValueUsage.None);
    }
    
    private BoundStmt BindVarDecl(VarDeclSyntax syntax)
    {
        var variableType = syntax.TypeAnnotation is not null
            ? BindType(syntax.TypeAnnotation, TypeUsage.Local).Type
            : null;
        
        var boundInitializer = BindVarDeclInitializer(syntax);
    
        if (variableType is null)
        {
            // Infer type
            variableType = boundInitializer.Type;
        }
        else
        {
            // Check type
            CheckType(boundInitializer, variableType);
        }

        // Accept shadowing only if it's another variable.
        var lookupResult = Scope.Lookup(syntax.Name.Identifier).FirstOrDefault();
        if (lookupResult is not (null or VariableSymbol))
            return BindError(syntax, new Diagnostic.CannotShadow(lookupResult, syntax.Name));
        
        var isReadOnly = syntax.VarOrLetKwToken.Kind is TokenKind.LetKw;
        var variable = new VariableSymbol(
            syntax.Name.Identifier, 
            Scope.OwnerAsFun ?? throw new InvalidOperationException("Values can only be bound on fun bodies."),
            isReadOnly, 
            variableType);
        SideEffects.AddResolvedSymbol(syntax.Name.Location, variable);
    
        return new BoundVarDecl(variable, boundInitializer, syntax);
    }
    
    private BoundValue BindVarDeclInitializer(VarDeclSyntax syntax)
    {
        if (syntax.Initializer is null)
            return BindError(syntax, new Diagnostic.MissingInitializer(syntax));
    
        return BindValue(syntax.Initializer, ValueUsage.Read);
    }
    
    private BoundStmt BindWhile(WhileStmtSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);
        var body = WithAdditionalFlags(BindingFlags.InsideLoop).BindValue(syntax.Body, ValueUsage.None);
        
        return new BoundWhile(condition, body, syntax);
    }
    
    #endregion

    
    private BoundNode BindExpr(ExprSyntax syntax, LookupOptions lookupOptions) => syntax switch
    {
        MemberAccessExprSyntax memberAccessExprSyntax => BindMemberAccess(memberAccessExprSyntax),
        IdNameSyntax idNameSyntax => BindIdName(idNameSyntax, lookupOptions),
        SelfSyntax selfSyntax => BindSelf(selfSyntax),
        CallExprSyntax callExprSyntax => BindCall(callExprSyntax),
        SubscriptExprSyntax subscriptExprSyntax => BindSubscript(subscriptExprSyntax),
        MutExprSyntax => BindError(syntax, new Diagnostic.MutNotAllowedHere(syntax)),
        
        // Strings and Literals
        NumberLiteralSyntax numberLiteralSyntax => BindNumberLiteral(numberLiteralSyntax),
        TrueLiteralSyntax => new BoundConst(value: true, type: Bool, syntax),
        FalseLiteralSyntax => new BoundConst(value: false, type: Bool, syntax),
        StringExprSyntax stringExprSyntax => BindString(stringExprSyntax),
        
        // Operators
        BinaryExprSyntax binaryExprSyntax => BindBinary(binaryExprSyntax),
        UnaryExprSyntax unaryExprSyntax => BindUnary(unaryExprSyntax),
        
        // Blocks and Control Flow
        BlockExprSyntax blockExprSyntax => BindBlock(blockExprSyntax),
        IfExprSyntax ifExprSyntax => BindIfExpr(ifExprSyntax),
        GroupExprSyntax groupExprSyntax => BindValue(groupExprSyntax.Inner, ValueUsage.Read),
        BreakExprSyntax breakExprSyntax => BindBreakOrContinue(breakExprSyntax),
        ContinueExprSyntax or BreakExprSyntax => BindBreakOrContinue(syntax),
        ReturnExprSyntax returnExprSyntax => BindReturn(returnExprSyntax),
        
        // Error
        ErrorExprSyntax errorExprSyntax => new BoundError(ErrorType, errorExprSyntax),
        FunNameSyntax => throw new UnreachableException(),
    };

    private BoundValue BindValue(ExprSyntax syntax, ValueUsage usage)
    {
        var bound = BindExpr(syntax, LookupOptions.Any);
        
        BoundValue value;
        if (bound is BoundValueOrTypeRef valueOrType)
            value = valueOrType.AsValue;
        else
        {
            if (bound is not BoundValue boundValue)
                return BindError(syntax, new Diagnostic.NotAValue(bound));
            value = boundValue;
        }

        if (value is BoundError || !CheckValueUsage(value, usage))
            return new BoundError(ErrorType, syntax);
        
        return value;
    }

    private bool CheckValueUsage(BoundValue value, ValueUsage usage)
    {
        if ((usage & ValueUsage.Assign) != 0 && value.PlaceKind is not PlaceKind.MutPlace)
        {
            SideEffects.ReportError(new Diagnostic.CannotAssign(value));
            return false;
        }

        if ((usage & ValueUsage.PassAsMut) != 0 && value.PlaceKind is not PlaceKind.MutPlace)
        {
            // + BindCall will do an exclusivity check on the arguments.
            SideEffects.ReportError(new Diagnostic.CannotPassAsMut(value));
            return false;
        }

        if ((usage & ValueUsage.ReturnAsMut) != 0)
        {
            if (value.PlaceKind is not PlaceKind.MutPlace)
            {
                SideEffects.ReportError(new Diagnostic.CannotReturnAsMut(value));
                return false;
            }
            
            Debug.Assert(value.PlaceRoot is not null);
            if (!CanMutReturnValueEscape(value.PlaceRoot))
            {
                SideEffects.ReportError(new Diagnostic.MutReturnEscapesScope(value));
                return false;
            }
        }

        return true;
    }

    private static bool CanMutReturnValueEscape(BoundValue root) => root switch
    {
        // Can escape: Parameters and mut return values of calls, where every argument
        // (including receiver) can escape.
        // Cannot escape: Locals
        
        BoundVariable
        {
            Variable: ParameterSymbol { IsMut: true }
        } => true, // Includes mut self, since that is a parameter
        BoundCall { IsMutReturn: true } call => call.ReceiverAndArguments
            .Where(arg => arg.IsMut)
            .All(arg => CanMutReturnValueEscape(arg.Value.PlaceRoot!)),

        _ => false,
    };
    
    public BoundTypeRef BindType(ExprSyntax syntax, TypeUsage usage)
    {
        var bound = BindExpr(syntax, LookupOptions.OnlyType);
        var typeRef = bound as BoundTypeRef;
        if (bound is BoundValueOrTypeRef valueOrType)
            typeRef = valueOrType.AsTypeRef;
        else if (bound is BoundTypeGroup typeGroup)
        {
            if (ResolveTypeGroup(typeGroup, [], null) is not BoundTypeRef boundTypeRef)
                return new BoundTypeRef(ErrorType, [], syntax);
            typeRef = boundTypeRef;
        }
        if (typeRef is null)
        {
            if (bound is not BoundError)
                SideEffects.ReportError(new Diagnostic.NotAType(bound));
            return new BoundTypeRef(ErrorType, [], syntax);
        }

        if (!CheckTypeUsage(typeRef, usage))
            return new BoundTypeRef(ErrorType, [], syntax);
        
        return typeRef;
    }

    private bool CheckTypeUsage(BoundTypeRef typeRef, TypeUsage usage)
    {
        if (usage is TypeUsage.Local)
            return true;

        // We must recurse into all type argument to check if they
        // are accessible.
        
        var requiredAccessibility = Scope.Owner.AccessibleWithin;
        return CheckRecursive(typeRef);
        
        bool CheckRecursive(BoundTypeRef currentRef)
        {
            if (!currentRef.Type.IsAccessibleFrom(requiredAccessibility))
            {
                SideEffects.DiagnosticBag.ReportError(new Diagnostic.InconsistentAccessibility(currentRef, Scope.Owner));
                return false;
            }
            
            return currentRef.TypeArguments.All(CheckRecursive);
        }
    }
    
    #region Literals and Strings and Self

    private BoundNode BindSelf(SelfSyntax syntax)
    {
        if (Scope.OwnerAsFun?.SelfParameter is null)
            return BindError(syntax, new Diagnostic.InvalidSelfRef(syntax));

        return new BoundVariable(Scope.OwnerAsFun.SelfParameter, syntax);
    }

    private BoundValue BindNumberLiteral(NumberLiteralSyntax syntax)
    {
        var type = syntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => Scope.Compilation.BaseLibrary.I32,
            NumberLiteralSuffix.I64 => Scope.Compilation.BaseLibrary.I64,
            NumberLiteralSuffix.F32 => Scope.Compilation.BaseLibrary.F32,
            NumberLiteralSuffix.F64 => Scope.Compilation.BaseLibrary.F64,

            _ => syntax.Token.HasDecimalPoint ? Scope.Compilation.BaseLibrary.DefaultFloatType : Scope.Compilation.BaseLibrary.DefaultIntType
        };

        // Literals like "1.1i32" need to be rejected.
        if (syntax.Token.HasDecimalPoint &&
            type != Scope.Compilation.BaseLibrary.F32 && type != Scope.Compilation.BaseLibrary.F64)
        {
            return BindError(syntax, new Diagnostic.SuffixInvalidForDecimalNumber(syntax));
        }

        if (type == Scope.Compilation.BaseLibrary.I32)
        {
            if (!int.TryParse(syntax.Token.Body, out var value))
                return BindError(syntax, new Diagnostic.NumberTooBig(type, syntax));

            return new BoundConst(value, type, syntax);
        }

        if (type == Scope.Compilation.BaseLibrary.I64)
        {
            if (!long.TryParse(syntax.Token.Body, out var value))
                return BindError(syntax, new Diagnostic.NumberTooBig(type, syntax));

            return new BoundConst(value, type, syntax);
        }

        if (type == Scope.Compilation.BaseLibrary.F32)
        {
            var value = float.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }

        if (type == Scope.Compilation.BaseLibrary.F64)
        {
            var value = double.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }

        throw new UnreachableException();
    }
    
    
    private BoundValue BindString(StringExprSyntax syntax)
    {
        var parts = syntax.Parts.Select(BindStringPart).ToImmutableArray();
    
        if (parts.Length == 0)
            return new BoundConst("", String, syntax);
        if (parts.Length == 1)
            return parts[0];

        if (parts.OfType<BoundError>().Any())
            return new BoundError(ErrorType, syntax);
        
        var concatCall = TryBindMethodCall(parts[0], Names.StringConcatMethod, [parts[1]], syntax);
        if (concatCall is null)
            return BindError(syntax, new Diagnostic.CannotConcatString(syntax, String));

        for (var i = 2; i < parts.Length; i++)
        {
            concatCall = TryBindMethodCall(concatCall, Names.StringConcatMethod, [parts[i]], syntax);
            if (concatCall is null)
                return BindError(syntax, new Diagnostic.CannotConcatString(syntax, String));
        }
        
        return concatCall;
    }
    
    private BoundValue BindStringPart(StringPartSyntax syntax)
        => syntax switch
        {
            StringTextSyntax textSyntax => new BoundConst(textSyntax.TextToken.ProcessedText, String, syntax),
            StringInterpolationSyntax interpolationSyntax => BindStringInterpolation(interpolationSyntax),
        };
    
    private BoundValue BindStringInterpolation(StringInterpolationSyntax syntax)
    {
        if (syntax.Expr is null)
        {
            // Empty interpolation means nothing will be added, which is equivalent
            // to an empty text const.
            return new BoundConst("", String, syntax);
        }
                
        var boundExpr = BindValue(syntax.Expr, ValueUsage.Read);

        if (IsAssignableTo(boundExpr.Type, String))
            return boundExpr;

        if (TryBindMethodCall(boundExpr, Names.ToStringMethod, [], syntax) is not BoundCall boundCall
            || boundCall.Type != String)
        {
            return BindError(syntax, new Diagnostic.CannotConvertToString(boundExpr));
        }

        return boundCall;
    }
    
    #endregion

    #region Operators and Assignment

    private BoundValue BindUnary(UnaryExprSyntax syntax)
        => BindSynthesizedOperatorCall(BindValue(syntax.Operand, ValueUsage.Read), syntax.Operator, [], syntax);
    
    private BoundValue BindBinary(BinaryExprSyntax syntax)
    {
        if (syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar)
            return BindLogicOperator(syntax);
        if (syntax.Operator.Kind == TokenKind.Equal)
            return BindAssign(syntax);
        
        var left = BindValue(syntax.Left, ValueUsage.Read);
        var right = BindValue(syntax.Right, ValueUsage.Read);

        return BindSynthesizedOperatorCall(left, syntax.Operator, [right], syntax);
    }

    private BoundValue BindSynthesizedOperatorCall(BoundValue receiver, Token @operator,
        ImmutableArray<BoundValue> arguments, SyntaxNode syntax)
    {
        if (TryBindMethodCall(receiver, @operator.Text.ToString(), arguments, syntax) is not BoundCall boundCall)
        {
            // If any operand had an error, stay silent.
            if (receiver.Type is ErrorTypeSymbol || arguments.Any(arg => arg.Type is ErrorTypeSymbol))
                return new BoundError(ErrorType, syntax);
            
            return BindError(syntax, new Diagnostic.UndefinedOperator(
                @operator,
                [receiver.Type, .. arguments.Select(arg => arg.Type)]));
        }

        SideEffects.AddResolvedSymbol(@operator.Location, boundCall.Fun);
        return boundCall;
    }

    private BoundValue BindLogicOperator(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar);
    
        var left = BindValue(syntax.Left, ValueUsage.Read);
        var right = BindValue(syntax.Right, ValueUsage.Read);
        if (left.Type is ErrorTypeSymbol || right.Type is ErrorTypeSymbol)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new BoundError(ErrorType, syntax);
        }
    
        // Type-check against bool
        if (!CheckType(left, Bool) ||
            !CheckType(right, Bool))
        {
            return new BoundError(ErrorType, syntax);
        }
    
        if (syntax.Operator.Kind is TokenKind.DoubleAmpersand)
            return new BoundLogicAnd(left, right, Bool, syntax);
        if (syntax.Operator.Kind is TokenKind.DoubleVerticalBar)
            return new BoundLogicOr(left, right, Bool, syntax);
    
        throw new UnreachableException();
    }

    private BoundValue BindAssign(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.Equal);

        var target = BindValue(syntax.Left, ValueUsage.Assign);
        var value = BindValue(syntax.Right, ValueUsage.Read);

        if (target is BoundError)
            return new BoundError(ErrorType, syntax);
        if (value.Type is ErrorTypeSymbol)
            return new BoundAssign(target, value, ErrorType, syntax);

        var type = CheckType(value, target.Type) ? Unit : ErrorType;
        return new BoundAssign(target, value, type, syntax);
    }
    
    #endregion
    
    #region Blocks, Control Flow
    
    private BoundBlock BindBlock(SyntaxNode syntax)
    {
        var inBlockBinder = EnterBlock();
        
        var stmts = inBlockBinder.BindStmts([.. syntax.SyntaxNodes()]);
        return new BoundBlock(stmts, Unit, syntax);
    }

    private ImmutableArray<BoundStmt> BindStmts(ImmutableArray<SyntaxNode> childNodes)
    {
        if (Scope is not BlockScope blockScope)
            throw new InvalidOperationException("Must be on block scope.");

        var stmts = ImmutableArray.CreateBuilder<BoundStmt>();
        for (var i = 0; i < childNodes.Length; i++)
        {
            var node = childNodes[i];
            if (node is StmtSyntax stmtSyntax)
            {
                var boundStmt = BindStmt(stmtSyntax);
                stmts.Add(boundStmt);
                if (boundStmt is BoundVarDecl boundVarDecl)
                {
                    // Create an adjacent scope and continue from there.
                    if (i + 1 < childNodes.Length)
                    {
                        var adjacentBinder = WithScope(blockScope.WithVariable(boundVarDecl.Variable));
                        stmts.AddRange(adjacentBinder.BindStmts(childNodes[(i + 1)..]));
                    }

                    return stmts.DrainToImmutable();
                }
            }
            else if (node is StructDeclSyntax or FunDeclSyntax)
            {
                // This is only allowed on scripts and is handled separately.
            }
            else if (node is NamespaceDeclSyntax)
                SideEffects.ReportError(new Diagnostic.NamespaceDeclarationInScript(node));
            else if (node is UsingDirectiveSyntax)
                SideEffects.ReportError(new Diagnostic.UsingDirectiveAfterCode(node));
            else
                SideEffects.ReportError(new Diagnostic.UnsupportedFeature(node));
        }

        return stmts.DrainToImmutable();
    }

    private BoundValue BindCondition(ExprSyntax syntax)
    {
        var condition = BindValue(syntax, ValueUsage.Read);
        if (!CheckType(condition, expected: Bool))
            condition = new BoundError(ErrorType, syntax);

        return condition;
    }
    
    private BoundValue BindIfExpr(IfExprSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);
        var body = BindValue(syntax.Body, ValueUsage.Read);
        var @else = BindElseExpr(syntax);

        // In some cases (e.g. Never type), one branch is assignable to
        // the other but not vice versa. Pick the one that works.
        TypeSymbol type;
        if (IsAssignableTo(source: @else.Type, target: body.Type))
            type = body.Type;
        else if (IsAssignableTo(source: body.Type, target: @else.Type))
            type = @else.Type;
        else
        {
            SideEffects.ReportError(new Diagnostic.IncompatibleBranches(body, @else));
            type = ErrorType;
        }
        
        return new BoundIfExpr(condition, body, @else, type, syntax);
    }

    private BoundValue BindElseExpr(IfExprSyntax ifSyntax)
    {
        var elseSyntax = ifSyntax.ElseBody;
        if (elseSyntax is null)
            return BindError(ifSyntax, new Diagnostic.MissingElse(ifSyntax));

        return BindValue(elseSyntax, ValueUsage.Read);
    }

    private BoundStmt BindIfStmt(IfExprSyntax syntax)
    {
        // If in stmt position does not require matching arm types
        // and always evaluates to unit.
        
        var condition = BindCondition(syntax.Condition);
        var body = BindExprStmt(syntax.Body);
        var @else = syntax.ElseBody is not null ? BindExprStmt(syntax.ElseBody) : null;
        
        return new BoundIfStmt(condition, body, @else, syntax);
    }
    
    private BoundValue BindBreakOrContinue(ExprSyntax syntax)
    {
        Debug.Assert(syntax is BreakExprSyntax or ContinueExprSyntax);
        
        if (!Scope.IsInsideLoop)
            return BindError(syntax, new Diagnostic.BreakOrContinueOutsideLoop(syntax));
    
        return syntax is BreakExprSyntax
            ? new BoundBreak(NeverType, syntax)
            : new BoundContinue(NeverType, syntax);
    }

    private BoundValue BindReturn(ReturnExprSyntax syntax)
    {
        if (Scope.OwnerAsFun is not FunSymbol fun)
            throw new InvalidOperationException("Values can only be bound on fun bodies.");

        if (syntax.Expr is null)
        {
            if (fun.ReturnType != Unit)
            {
                SideEffects.ReportError(new Diagnostic.MissingReturnValue(fun, syntax));
                return new BoundReturn(new BoundError(ErrorType), isMut: false, NeverType, syntax);
            }
            return new BoundReturn(null, isMut: false, NeverType, syntax);
        }

        var value = BindReturnValue(syntax.Expr, out var isMutReturn);
        return new BoundReturn(value, isMutReturn, NeverType, syntax);
    }

    private BoundValue BindReturnValue(ExprSyntax syntax, out bool isMutReturn)
    {
        if (Scope.OwnerAsFun is not FunSymbol fun)
            throw new InvalidOperationException("Return values can only be bound on fun bodies.");

        if (syntax is MutExprSyntax mutExprSyntax)
        {
            if (!fun.IsReturnMut)
            {
                isMutReturn = false;
                return BindError(syntax, new Diagnostic.UnexpectedMutReturn(mutExprSyntax, fun));
            }

            isMutReturn = true;

            var mutValue = BindValue(mutExprSyntax.Expr, ValueUsage.ReturnAsMut);
            if (!CheckType(mutValue, fun.ReturnType))
                return new BoundError(ErrorType, mutExprSyntax.Expr);
            return mutValue;
        }

        if (fun.IsReturnMut)
        {
            isMutReturn = false;
            return BindError(syntax, new Diagnostic.MissingMutReturn(syntax, fun));
        }

        isMutReturn = false;
        var value = BindValue(syntax, ValueUsage.Read);
        if (!CheckType(value, fun.ReturnType))
            return new BoundError(ErrorType, syntax);
        return value;
    }

    #endregion

    #region Names and Member access
    
    private BoundNode BindIdName(IdNameSyntax syntax, LookupOptions lookupOptions)
    {
        if (syntax.Token.IsMissing)
            return new BoundError(ErrorType, syntax);

        var lookupResult = LookupName(syntax.Token.Identifier, lookupOptions);

        if (lookupResult.Viability is LookupViability.NotFound)
            return BindError(syntax, new Diagnostic.UndefinedName(syntax));

        if (lookupResult.Viability is LookupViability.NotAccessible)
            return BindError(syntax, new Diagnostic.NotAccessible(lookupResult.Symbols, syntax));

        var symbol = lookupResult.Symbols[0];
        if (lookupResult.Symbols.All(s => s is FunSymbol))
            return BindFunGroup(lookupResult, receiver: null, syntax);
        if (lookupResult.Symbols.All(s => s is TypeSymbol) && lookupResult.Symbols.Length > 1 ||
            symbol is TypeSymbol { TypeParameters.Length: > 0 })
        {
            // Could be concrete, but many means it could be generics which need
            // "overload" resolution.
            return new BoundTypeGroup([.. lookupResult.Symbols.Cast<TypeSymbol>()], syntax);
        }

        if (lookupResult.Symbols.Length > 1)
            return BindError(syntax, new Diagnostic.AmbiguousReference(lookupResult, syntax));

        var boundSymbol = symbol switch
        {
            NamespaceSymbol @namespace => new BoundNamespaceRef(@namespace, syntax),
            TypeSymbol type => new BoundTypeRef(type, typeArguments: [], syntax),

            VariableSymbol variable => BindVariable(variable, syntax),
            FunSymbol => BindFunGroup(lookupResult, receiver: null, syntax),
            FieldSymbol field => BindFieldAccess(field, null, syntax)
        };

        // Note that the symbol that was bound might differ from the symbol
        // we found, because e.g. field access might do another lookup for
        // a type of the same name.
        if (boundSymbol is IBoundSymbolRef symbolRef)
            SideEffects.AddResolvedSymbol(syntax.Location, symbolRef.Symbol);
            
        if (lookupResult.Viability is LookupViability.WrongKind)
            return BindIdNameWithWrongKind(boundSymbol, lookupOptions);

        return boundSymbol;
    }

    private BoundError BindIdNameWithWrongKind(BoundNode boundNode, LookupOptions lookupOptions)
        => lookupOptions switch
        {
            { } when boundNode is BoundError error => error,
            LookupOptions.MustBeCallable => BindError(boundNode.Syntax!, new Diagnostic.CannotCall(boundNode)),
            LookupOptions.OnlyType => BindError(boundNode.Syntax!, new Diagnostic.NotAType(boundNode)),
            LookupOptions.OnlyNamespace => BindError(boundNode.Syntax!, new Diagnostic.NotANamespace(boundNode)),
            _ => throw new UnreachableException($"{nameof(LookupOptions)}.{lookupOptions} is unknown to {nameof(BindIdNameWithWrongKind)}")
        };
    

    private BoundNode BindMemberAccess(MemberAccessExprSyntax syntax)
    {
        var left = BindExpr(syntax.Left, LookupOptions.Any);

        if (syntax.Member.Token.IsMissing)
            return new BoundError(ErrorType, syntax.Member);
        
        if (left is BoundFunGroup leftAsFunGroup)
            return BindError(syntax, new Diagnostic.FunHasNoMembers(leftAsFunGroup, syntax.Member));
        if (left is BoundTypeGroup typeGroup)
        {
            if (ResolveTypeGroup(typeGroup, [], null) is not BoundTypeRef resolvedTypeRef)
                return new BoundError(ErrorType, syntax);
            left = resolvedTypeRef;
        }
        
        NamespaceOrTypeSymbol namespaceOrType = left switch
        {
            // Note that BoundValueOrTypeRef goes through BoundValue here
            // and returns it's type.
            BoundValue value => value.Type,
            
            BoundNamespaceRef boundNamespaceRef => boundNamespaceRef.Namespace,
            BoundTypeRef boundTypeRef => boundTypeRef.Type,
            
            BoundFunGroup => throw new UnreachableException("Fun group handled above."),
            BoundTypeGroup => throw new UnreachableException("Type group handled above."),
            BoundStmt => throw new UnreachableException("Stmt can never be the left side of member access."),
        };
        if (namespaceOrType is ErrorTypeSymbol)
            return new BoundError(ErrorType, syntax.Member);

        var memberName = syntax.Member.Text.ToString();
        var lookupResult = LookupMembers(namespaceOrType, memberName);

        if (lookupResult.Viability is LookupViability.NotFound)
            return BindError(syntax, new Diagnostic.UndefinedMember(left, syntax.Member));

        if (lookupResult.Viability is LookupViability.NotAccessible)
            return BindError(syntax, new Diagnostic.NotAccessible(lookupResult.Symbols, syntax.Member));
        
        var symbol = lookupResult.Symbols[0];
        if (lookupResult.Symbols.All(s => s is FunSymbol))
            return BindFunGroup(lookupResult, receiver: left, syntax.Member);
        if (lookupResult.Symbols.All(s => s is TypeSymbol) && lookupResult.Symbols.Length > 1 ||
            symbol is TypeSymbol { TypeParameters.Length: > 0 })
        {
            // Could be concrete, but many means it could be generics which need
            // "overload" resolution.
            return new BoundTypeGroup([.. lookupResult.Symbols.Cast<TypeSymbol>()], syntax.Member);
        }
        
        
        if (lookupResult.Symbols.Length > 1)
            return BindError(syntax, new Diagnostic.AmbiguousReference(lookupResult, syntax.Member));
        
        var receiver = ReplaceBoundValueOrTypeRef(left, symbol);
        SideEffects.AddResolvedSymbol(syntax.Member.Location, symbol);
        
        return symbol switch
        {
            NamespaceSymbol @namespace => new BoundNamespaceRef(@namespace, syntax.Member),
            TypeSymbol type => new BoundTypeRef(type, typeArguments: [], syntax.Member),
            // Note that type argument from the left side are already substituted.

            VariableSymbol => throw new UnreachableException("Variables are never members."),
            FunSymbol => throw new UnreachableException("Handled above"),
            FieldSymbol field => BindFieldAccess(field, receiver, syntax.Member),
        };
    }

    private BoundNode ReplaceBoundValueOrTypeRef(BoundNode receiver, Symbol symbol)
    {
        if (receiver is not BoundValueOrTypeRef valueOrType)
            return receiver;

        BoundNode replaced = symbol switch
        {
            // Fields are instance members.
            // Variables should not be here, but for resilience, return as value anyway.
            FieldSymbol or VariableSymbol => valueOrType.AsValue,
            
            // Funs can be static or instance members.
            FunSymbol fun => fun.IsStatic ? valueOrType.AsTypeRef : valueOrType.AsValue,
            
            // Types are always accessed through the type.
            NamespaceOrTypeSymbol => valueOrType.AsTypeRef,
        };

        if (replaced is IBoundSymbolRef symbolRef)
            SideEffects.AddResolvedSymbol(replaced.Syntax!.Location, symbolRef.Symbol);

        return replaced;
    }
    
    private BoundNode BindVariable(VariableSymbol variable, IdNameSyntax syntax)
    {
        // Variables could be captured from outside, which is forbidden.

        if (!Equals(variable.Owner, Scope.Owner))
            return BindError(syntax, new Diagnostic.CannotCapture(syntax));

        return new BoundVariable(variable, syntax);
    }

    private BoundNode BindFunGroup(LookupResult lookupResult, BoundNode? receiver, IdNameSyntax syntax)
    {
        if (lookupResult.Symbols.Any(s => s is not FunSymbol))
        {
            Debug.Assert(lookupResult.Symbols.Length > 1);
            return BindError(syntax, new Diagnostic.AmbiguousReference(lookupResult, syntax));
        }

        return new BoundFunGroup([.. lookupResult.Symbols.Cast<FunSymbol>()], receiver, syntax);
    }

    private BoundNode BindFieldAccess(FieldSymbol field, BoundNode? receiver, IdNameSyntax syntax)
    {
        // A field name could still be interpreted as a type ref if
        // the field is named after its type.
        BoundTypeRef? asTypeRef = null;
        if (syntax.Text.SequenceEqual(field.Type.Name))
        {
            // Try to bind it as type as well.
            var typeLookup = LookupName(syntax.Text.ToString(), LookupOptions.OnlyType);
            if (typeLookup.SingleViable is TypeSymbol { TypeParameters.Length: 0 } type)
                asTypeRef = new BoundTypeRef(type, typeArguments: [], syntax);
        }

        if (receiver is null)
        {
            // Field was bound as a plain name.
            // It can be interpreted as type (if possible) or with an implicit self.
            if (Scope.OwnerAsFun?.SelfParameter?.Type is null)
            {
                return asTypeRef is null
                    ? BindError(syntax, new Diagnostic.InstanceMemberInStaticContext(field, Scope.Owner, syntax))
                    : asTypeRef;
            }

            if (!Equals(Scope.OwnerAsFun.SelfParameter.Type, field.OwningType))
            {
                return asTypeRef is null
                    ? BindError(syntax, new Diagnostic.MemberOfEnclosingType(field, Scope.OwnerAsFun.SelfParameter.Type, syntax))
                    : asTypeRef;
            }

            receiver = new BoundVariable(Scope.OwnerAsFun.SelfParameter);    
        }

        if (receiver is not BoundValue valueReceiver)
            return BindError(syntax, new Diagnostic.InstanceMemberThroughType(field, syntax));

        var asField = new BoundField(valueReceiver, field, syntax);
        return asTypeRef is not null ? new BoundValueOrTypeRef(asField, asTypeRef) : asField;
    }

    #endregion
    
    #region Subscripts

    private BoundNode BindSubscript(SubscriptExprSyntax syntax)
    {
        var left = BindExpr(syntax.Left, LookupOptions.Any);

        if (left is BoundError)
            return new BoundError(ErrorType, syntax);

        switch (left)
        {
            case BoundFunGroup { HasTypeArguments: false } funGroup:
            {
                var typeArgs = syntax.ArgList.Arguments
                    .Select(s => BindType(s, TypeUsage.Local))
                    .ToImmutableArray();

                return funGroup.WithTypeArguments(typeArgs, syntax.ArgList);
            }

            case BoundTypeGroup typeGroup:
            {
                var typeArgs = syntax.ArgList.Arguments
                    .Select(arg => BindType(arg, TypeUsage.Local))
                    .ToImmutableArray();
                return ResolveTypeGroup(typeGroup, typeArgs, syntax.ArgList);
            }

            case BoundTypeRef { Type.TypeParameters.Length: 0 } nonGenericTypeRef:
                return BindError(syntax.ArgList, new Diagnostic.NotGeneric([nonGenericTypeRef.Type], syntax.ArgList));

            default:
                return BindError(syntax.ArgList, new Diagnostic.SubscriptNotAllowed(syntax.ArgList, left));

        }
    }

    private BoundNode ResolveTypeGroup(BoundTypeGroup group, ImmutableArray<BoundTypeRef> typeArguments, SyntaxNode? typeArgumentsSyntax)
    {
        Debug.Assert(typeArguments.Length == 0 || typeArgumentsSyntax is not null);
        if (typeArguments.Any(a => a.Type is ErrorTypeSymbol))
        {
            if (group.Candidates.Length == 1)
                SideEffects.AddResolvedSymbol(group.Syntax!.Location, group.Candidates[0]);
            return new BoundError(ErrorType, group.Syntax);
        }

        var candidates = group.Candidates
            .Where(t => t.TypeParameters.Length == typeArguments.Length)
            .ToImmutableArray();
        if (candidates.Length == 0)
        {
            // If there was only one, register the symbol for good syntax highlighting.
            // Otherwise, we don't know which one it was.
            if (group.Candidates.Length == 1)
                SideEffects.AddResolvedSymbol(group.Syntax!.Location, group.Candidates[0]);

            if (typeArguments.Length == 0)
            {
                Debug.Assert(group.Candidates.All(t => t.TypeParameters.Length > 0));
                return BindError(group.Syntax!, new Diagnostic.MissingTypeArguments([.. group.Candidates], group.Syntax!));
            }
            
            if (group.Candidates.All(t => t.TypeParameters.Length == 0))
            {
                // This only happens, when there are multiple declarations without type parameters.
                // Still NotGeneric is the more useful error than AmbiguousReference.
                Debug.Assert(typeArguments.Length > 0 && typeArgumentsSyntax is not null);
                return BindError(group.Syntax!, new Diagnostic.NotGeneric([.. group.Candidates], typeArgumentsSyntax));
            }
            
            return BindError(group.Syntax!, new Diagnostic.WrongTypeArgumentCount(
                [.. group.Candidates], typeArguments.Length, typeArgumentsSyntax!));
        }

        if (candidates.Length > 1)
        {
            // This only happens, when there are clashing declaration, which already
            // produce an error.
            return BindError(group.Syntax!, new Diagnostic.AmbiguousReference(group.Candidates.CastArray<Symbol>(),
                group.Syntax!));
        }


        var specializedType = candidates[0].Specialize([.. typeArguments.Select(t => t.Type)]);
        SideEffects.AddResolvedSymbol(group.Syntax!.Location, specializedType);
        return new BoundTypeRef(specializedType, typeArguments, group.Syntax);
    }
    
    #endregion 
    
    #region Call and Type Initializer

    /// <summary>
    /// How far a fun overload candidate got before it stopped matching the call.
    /// A higher value is a better match.
    /// </summary>
    private enum FunApplicability
    {
        WrongTypeArgumentCount,
        WrongArgumentCount,
        CannotInferTypeArguments,
        ArgumentMismatch,
        Applicable,
    }

    /// <summary>
    /// Everything overload resolution needs to know about one call.
    /// For synthesized calls (operators, string interpolation) there is no
    /// callee or argument list syntax, so everything points at <see cref="Syntax"/>.
    /// </summary>
    private sealed record CallSite(
        BoundFunGroup Callee,
        ImmutableArray<BoundArgument> Arguments,
        SyntaxNode Syntax,
        SyntaxNode ArgListSyntax)
    {
        public SyntaxNode CalleeSyntax => Callee.Syntax ?? Syntax;
        public SyntaxNode? TypeArgListSyntax => Callee.TypeArgListSyntax;
        public bool HasErrorArgument => Arguments.Any(a => a.Type is ErrorTypeSymbol);
    }

    /// <summary>
    /// The result of checking one candidate against a call.
    /// <see cref="Fun"/> is specialized if the candidate got past the type argument check.
    /// <see cref="SideEffects"/> belongs to this candidate only: it holds the diagnostics
    /// explaining why it doesn't fit, and is only merged if this candidate is the
    /// single best one.
    /// </summary>
    private sealed record Candidate(FunSymbol Fun, FunApplicability Applicability, BinderSideEffects SideEffects);

    /// <summary>
    /// Binds a call written in source. Reports every error.
    /// </summary>
    private BoundValue BindFunCall(CallSite site)
    {
        var best = ResolveOverloads(site);

        // One best candidate: either the winner, or the closest miss.
        // Either way its own diagnostics are the most specific ones we have.
        if (best is [var only])
        {
            SideEffects.AddResolvedSymbol(site.CalleeSyntax.Location, only.Fun);
            only.SideEffects.DrainInto(SideEffects);
            return only.Applicability is FunApplicability.Applicable
                ? CompleteCall(only.Fun, site)
                : new BoundError(ErrorType, site.Syntax);
        }

        // Several candidates are equally good (or equally bad).
        // With an error argument we can't tell them apart, so stay silent.
        var applicability = best[0].Applicability;
        if (!site.HasErrorArgument || applicability < FunApplicability.ArgumentMismatch)
            SideEffects.ReportError(MakeResolutionError(applicability, [.. best.Select(c => c.Fun)], site));

        return new BoundError(ErrorType, site.Syntax);
    }

    /// <summary>
    /// Checks every candidate on its own throwaway side effects and returns
    /// the ones that got furthest. Reports nothing to this binder.
    /// </summary>
    private ImmutableArray<Candidate> ResolveOverloads(CallSite site)
    {
        Debug.Assert(site.Callee.Candidates.Length > 0);

        var best = ImmutableArray.CreateBuilder<Candidate>();
        foreach (var fun in site.Callee.Candidates)
        {
            var candidate = WithSideEffects(new BinderSideEffects()).CheckFunCandidate(fun, site);

            if (best.Count > 0)
            {
                if (candidate.Applicability < best[0].Applicability)
                    continue;
                if (candidate.Applicability > best[0].Applicability)
                    best.Clear();
            }

            best.Add(candidate);
        }

        return best.DrainToImmutable();
    }

    /// <summary>
    /// Checks one candidate against the call. Diagnostics go into this binder's
    /// side effects, which belong to the candidate (see <see cref="ResolveOverloads"/>).
    /// </summary>
    private Candidate CheckFunCandidate(FunSymbol fun, CallSite site)
    {
        var typeArguments = site.Callee.TypeArguments.Select(t => t.Type).ToImmutableArray();

        if (typeArguments.Length == 0 && fun.TypeParameters.Length > 0)
        {
            // Infer type arguments only if arity matches, otherwise
            // it doesn't make sense.
            if (fun.Parameters.Length != site.Arguments.Length)
                return Fail(FunApplicability.WrongArgumentCount);

            var inferred = InferTypeArguments(fun.TypeParameters,
                [.. fun.Parameters.Select(p => p.Type)], 
                [.. site.Arguments.Select(a => a.Type)]);
            if (inferred is null)
                return Fail(FunApplicability.CannotInferTypeArguments);
            typeArguments = inferred.Value;
        }
        else if (fun.TypeParameters.Length != typeArguments.Length)
            return Fail(FunApplicability.WrongTypeArgumentCount);
        
        fun = fun.Specialize(typeArguments);

        if (fun.Parameters.Length != site.Arguments.Length)
            return Fail(FunApplicability.WrongArgumentCount);

        var applicability = FunApplicability.Applicable;
        for (var i = 0; i < site.Arguments.Length; i++)
        {
            var argument = site.Arguments[i];
            var parameter = fun.Parameters[i];
            if (!IsAssignableTo(argument.Type, parameter.Type) || argument.IsMut != parameter.IsMut)
            {
                SideEffects.ReportError(new Diagnostic.ArgumentTypeMismatch(argument, parameter));
                applicability = FunApplicability.ArgumentMismatch;
            }
        }

        return new Candidate(fun, applicability, SideEffects);

        Candidate Fail(FunApplicability reason)
        {
            SideEffects.ReportError(MakeResolutionError(reason, [fun], site));
            return new Candidate(fun, reason, SideEffects);
        }
    }

    /// <summary>
    /// Tries to fill all <paramref name="typeParameters"/> by collecting hints from
    /// <paramref name="arguments"/> that are assigned to <paramref name="parameters"/>.
    /// Expected that <paramref name="arguments"/> and <paramref name="parameters"/> have
    /// the same length. Returns an array of same length as <paramref name="typeParameters"/>
    /// if inference was successful, otherwise <c>null</c>.
    /// </summary>
    private ImmutableArray<TypeSymbol>? InferTypeArguments(
        ImmutableArray<TypeParameterSymbol> typeParameters,
        ImmutableArray<TypeSymbol> parameters, 
        ImmutableArray<TypeSymbol> arguments)
    {
        var hints = Enumerable
            .Range(0, Math.Min(parameters.Length, arguments.Length))
            .SelectMany(i =>
                CollectTypeArgumentHints(
                    target: parameters[i],
                    argument: arguments[i]));

        var hintsByParam = new Dictionary<TypeParameterSymbol, TypeSymbol>();
        foreach (var hint in hints)
        {
            if (!typeParameters.Contains(hint.Param))
            {
                // The hint belongs to a different type param.
                continue;
            }

            // Don't infer never or error
            if (hint.Type is ErrorTypeSymbol || hint.Type == NeverType)
                continue;

            if (!hintsByParam.TryAdd(hint.Param, hint.Type))
            {
                // It's already there, they must be assignable.
                // Swap if this hint is better.
                var lastHintArgument = hintsByParam[hint.Param];
                if (IsAssignableTo(hint.Type, lastHintArgument))
                    continue;
                if (IsAssignableTo(lastHintArgument, hint.Type))
                    hintsByParam[hint.Param] = lastHintArgument;
                else
                    return null;
            }
        }
        
        var typeArgs = ImmutableArray.CreateBuilder<TypeSymbol>();
        foreach (var param in typeParameters)
        {
            if (!hintsByParam.TryGetValue(param, out var argumentHint))
                return null;
            typeArgs.Add(argumentHint);
        }
        
        return typeArgs.DrainToImmutable();
    }

    private IEnumerable<(TypeParameterSymbol Param, TypeSymbol Type)> CollectTypeArgumentHints(
        TypeSymbol target, TypeSymbol argument)
    {
        // These could be T <- I32, Pair[T, U] <- Pair[I32, F64]
        // or don't match at all.
        if (target is TypeParameterSymbol targetParam)
            return [(targetParam, argument)];

        if (Equals(target.OriginalDefinition, argument.OriginalDefinition))
        {
            return Enumerable.Range(0, target.TypeArguments.Length)
                .SelectMany(i => CollectTypeArgumentHints(target.TypeArguments[i], argument.TypeArguments[i]));
        }

        return [];
    }


    /// <summary>
    /// The error for one or more candidates that all stopped at the same step.
    /// Used for a single candidate as well as for a tie, so each error lives in one place.
    /// For <see cref="FunApplicability.Applicable"/> it's the ambiguity error.
    /// </summary>
    private static Diagnostic.Error? MakeResolutionError(FunApplicability applicability,
        ImmutableArray<FunSymbol> funs, CallSite site) => applicability switch
    {
        FunApplicability.WrongTypeArgumentCount => MakeTypeArgumentCountError(funs, site),
        FunApplicability.WrongArgumentCount => new Diagnostic.WrongArgumentCount(
            [.. funs], site.Arguments.Length, site.ArgListSyntax),

        FunApplicability.CannotInferTypeArguments => site.Arguments.All(a => a.Type is not ErrorTypeSymbol)
            ? new Diagnostic.CannotInferTypeArguments([.. funs], site.CalleeSyntax)
            : null,

        FunApplicability.ArgumentMismatch => site.Arguments.All(a => a.Type is not ErrorTypeSymbol)
            ? funs.Length > 1
                ? new Diagnostic.NoMatchingOverload([.. funs], site.Arguments, site.ArgListSyntax)
                : throw new UnreachableException("Single argument mismatches are reported in candidate checking.")
            : null,

        FunApplicability.Applicable => new Diagnostic.AmbiguousCall([.. funs], site.CalleeSyntax),
        _ => throw new UnreachableException(),
    };

    private static Diagnostic.Error MakeTypeArgumentCountError(ImmutableArray<FunSymbol> funs, CallSite site)
    {
        var count = site.Callee.TypeArguments.Length;
        if (count == 0)
        {
            Debug.Assert(funs.All(f => f.TypeParameters.Length > 0));
            return new Diagnostic.MissingTypeArguments([.. funs], site.CalleeSyntax);
        }

        Debug.Assert(site.TypeArgListSyntax is not null);
        if (funs.All(f => f.TypeParameters.Length == 0))
            return new Diagnostic.NotGeneric([.. funs], site.TypeArgListSyntax);

        return new Diagnostic.WrongTypeArgumentCount([.. funs], count, site.TypeArgListSyntax);
    }

    /// <summary>
    /// Binds the receiver for the chosen fun and builds the call. Runs after
    /// overload resolution, so receiver errors never decide which fun wins.
    /// </summary>
    private BoundValue CompleteCall(FunSymbol fun, CallSite site)
    {
        var receiver = BindCallReceiver(fun, site.Callee.Receiver, site.CalleeSyntax);
        if (receiver is BoundError)
            return new BoundError(ErrorType, site.Syntax);

        var receiverArgument = receiver is not null
            ? new BoundArgument(null, receiver, isMut: fun.SelfParameter!.IsMut, null)
            : null;
        return new BoundCall(fun, receiverArgument, site.Arguments, site.Syntax);
    }

    /// <summary>
    /// Returns the value receiver for <paramref name="fun"/>, <c>null</c> for static funs,
    /// or a <see cref="BoundError"/> (after reporting) if the receiver doesn't fit.
    /// Uses an implicit self if no receiver is given and the fun is an instance fun.
    /// </summary>
    private BoundValue? BindCallReceiver(FunSymbol fun, BoundNode? receiverNode, SyntaxNode calleeSyntax)
    {
        if (receiverNode is not null)
            receiverNode = ReplaceBoundValueOrTypeRef(receiverNode, fun);

        // Try an implicit self, if possible (fun is not static, no receiver specified, self available)
        var valueReceiver = !fun.IsStatic && receiverNode is null &&
                            Scope.OwnerAsFun?.SelfParameter is not null
            ? new BoundVariable(Scope.OwnerAsFun.SelfParameter)
            : receiverNode as BoundValue;

        if (valueReceiver is null != fun.IsStatic)
        {
            // Switch on the original receiver node to get more
            // specific errors.
            SideEffects.ReportError(receiverNode switch
            {
                null => new Diagnostic.InstanceMemberInStaticContext(
                    fun,
                    Scope.Owner,
                    calleeSyntax),
                not BoundValue => new Diagnostic.InstanceMemberThroughType(
                    fun,
                    calleeSyntax),
                BoundValue => new Diagnostic.StaticMemberThroughInstance(
                    fun,
                    calleeSyntax)
            });
            return new BoundError(ErrorType);
        }

        if (valueReceiver is null)
            return null;

        // See, if the receiver is valid
        if (!Equals(fun.SelfParameter!.Type, valueReceiver.Type))
        {
            SideEffects.ReportError(new Diagnostic.MemberOfEnclosingType(fun, Scope.InstanceType!, calleeSyntax));
            return new BoundError(ErrorType);
        }

        if (fun.SelfParameter.IsMut && valueReceiver.PlaceKind is not PlaceKind.MutPlace)
        {
            SideEffects.ReportError(new Diagnostic.CannotMutateReceiver(valueReceiver, fun, Scope.Owner, calleeSyntax));
            return new BoundError(ErrorType);
        }

        return valueReceiver;
    }


    private BoundValue BindCall(CallExprSyntax syntax)
    {
        var callee = BindExpr(syntax.Callee, LookupOptions.MustBeCallable);
        
        var arguments = syntax.ArgList.Arguments.Select(BindArgument).ToImmutableArray();
        
        if (callee is BoundTypeRef typeRef)
            return BindTypeInit(typeRef, arguments, syntax);
        if (callee is BoundTypeGroup typeGroup)
            return BindTypeInit(typeGroup, arguments, syntax);
        
        //TODO: Implement named arguments
        if (syntax.ArgList.Arguments.Any(argSyntax => argSyntax.IsNamed))
        {
            return BindError(syntax,
                new Diagnostic.UnsupportedFeature(syntax.ArgList, "Named arguments are not yet implemented."));
        }

        if (callee is BoundError)
            return new BoundError(ErrorType, syntax);
        
        if (callee is not BoundFunGroup calleeFunGroup)
            return BindError(syntax, new Diagnostic.CannotCall(callee));

        return BindFunCall(new CallSite(calleeFunGroup, arguments, syntax, syntax.ArgList));
    }

    private BoundArgument BindArgument(ArgSyntax syntax)
    {
        if (syntax.Expr is MutExprSyntax mutExpr)
        {
            var value = BindValue(mutExpr.Expr, ValueUsage.PassAsMut);
            return new BoundArgument(syntax.Name, value, isMut: true, syntax);
        }
        return new BoundArgument(syntax.Name, BindValue(syntax.Expr, ValueUsage.Read), isMut: false, syntax);
    }

    /// <summary>
    /// Looks up <paramref name="methodName"/> on <paramref name="receiver"/> and tries
    /// to bind a synthesized call to it (operators, string concat, ToString).
    /// Returns <c>null</c> if it cannot, and does _not_ report any errors.
    /// </summary>
    private BoundCall? TryBindMethodCall(BoundValue receiver, string methodName,
        ImmutableArray<BoundValue> arguments,
        SyntaxNode syntax)
    {
        if (receiver.Type is ErrorTypeSymbol)
            return null;

        var lookup = LookupMembers(receiver.Type, methodName);
        if (lookup.Viability is LookupViability.NotFound or LookupViability.NotAccessible)
            return null;

        var funCandidates = lookup.Symbols.OfType<FunSymbol>().ToImmutableArray();
        if (funCandidates.Length == 0)
            return null;

        // Synthesized calls have no callee or argument list syntax of their own,
        // and never pass anything as mut.
        var site = new CallSite(
            new BoundFunGroup(funCandidates, receiver),
            [.. arguments.Select(a => new BoundArgument(null, a, isMut: false, null))],
            Syntax: syntax,
            ArgListSyntax: syntax);

        if (ResolveOverloads(site) is not [{ Applicability: FunApplicability.Applicable } winner])
            return null;

        // Receiver checks still report, so run them on a scratch binder
        // and only keep the result if they pass.
        var receiverBinder = WithSideEffects(new BinderSideEffects());
        if (receiverBinder.CompleteCall(winner.Fun, site) is not BoundCall call)
            return null;

        winner.SideEffects.DrainInto(SideEffects);
        receiverBinder.SideEffects.DrainInto(SideEffects);
        return call;
    }



    /// <summary>
    /// How far a candidate for type group resolution got before it
    /// stopped matching. A higher value is a better match.
    /// </summary>
    private enum TypeApplicability
    {
        CannotInitialize,
        FieldNameMismatch,
        CannotInferTypeArguments,
        Applicable
    }

    private sealed record TypeCandidate(
        TypeSymbol Type,
        TypeApplicability Applicability,
        BinderSideEffects SideEffects);
    
    
    private BoundValue BindTypeInit(BoundNode typeGroupOrSingle, ImmutableArray<BoundArgument> arguments,
        CallExprSyntax syntax)
    {
        Debug.Assert(typeGroupOrSingle is BoundTypeGroup or BoundTypeRef);

        // Arguments must all be named and distinct
        var missingNameArguments = arguments.Where(a => a.FieldNameSyntax is null).ToImmutableArray();
        if (missingNameArguments.Length > 0)
        {
            foreach (var arg in missingNameArguments)
                SideEffects.ReportError(new Diagnostic.MissingFieldName(arg.Syntax!));
            return new BoundError(ErrorType, syntax);
        }

        var duplicateNames = arguments
            .GroupBy(a => a.FieldNameSyntax!.Text.ToString())
            .Select(grp => grp.ToImmutableArray())
            .Where(grp => grp.Length > 1)
            .ToImmutableArray();
        if (duplicateNames.Length > 0)
        {
            foreach (var duplicates in duplicateNames)
                SideEffects.ReportError(new Diagnostic.DuplicateArgumentNames(duplicates));
            return new BoundError(ErrorType, syntax);
        }
        
        // Single
        if (typeGroupOrSingle is BoundTypeRef typeRef)
        {
            var candidate = CheckTypeCandidate(typeRef.Type, arguments, inferTypeArguments: false, typeRef.Syntax!, syntax);
            if (candidate.Applicability is not TypeApplicability.Applicable)
                return new BoundError(ErrorType, syntax);
            return CompleteTypeInit(candidate, arguments, syntax);
        }

        // Type group resolution
        var typeGroup = (BoundTypeGroup)typeGroupOrSingle;
        var candidates = new List<TypeCandidate>();

        foreach (var type in typeGroup.Candidates)
        {
            var binder = WithSideEffects(new BinderSideEffects());
            var candidate = binder.CheckTypeCandidate(type, arguments, inferTypeArguments: true, typeGroup.Syntax!, syntax);
            if (candidates.Count == 0 || candidate.Applicability == candidates[0].Applicability)
                candidates.Add(candidate);
            else if (candidate.Applicability > candidates[0].Applicability)
            {
                candidates.Clear();
                candidates.Add(candidate);
            }
        }

        Debug.Assert(candidates.Count > 0);
        if (candidates.Count == 1)
        {
            var candidate = candidates[0];
            candidate.SideEffects.DrainInto(SideEffects);
            if (candidate.Applicability is not TypeApplicability.Applicable)
                return new BoundError(ErrorType, syntax);
            return CompleteTypeInit(candidate, arguments, syntax);
        }

        return BindError(syntax, MakeTypeResolutionError([.. candidates.Select(c => c.Type)], candidates[0].Applicability, typeGroup.Syntax!, syntax));
    }

    private TypeCandidate CheckTypeCandidate(TypeSymbol type, ImmutableArray<BoundArgument> arguments, bool inferTypeArguments, SyntaxNode typeSyntax,
        CallExprSyntax syntax)
    {
        if (type.OriginalDefinition is not StructSymbol originalDefinition ||
            originalDefinition.IsPrimitive)
            return Fail(TypeApplicability.CannotInitialize);

        // All fields bound and none left out?
        var fieldPerArgument = BindFieldsToArguments(arguments, type);
        if (fieldPerArgument is null)
            return Fail(TypeApplicability.FieldNameMismatch);

        var uninitializedFields = type.Members.OfType<FieldSymbol>()
            .Where(f => !fieldPerArgument.Value.Contains(f))
            .ToImmutableArray();
        if (uninitializedFields.Length > 0)
        {
            SideEffects.ReportError(new Diagnostic.MissingFieldInitializers(uninitializedFields, syntax.ArgList));
            return new TypeCandidate(type, TypeApplicability.FieldNameMismatch, SideEffects);
        }
        
        // Infer type arguments if necessary
        if (inferTypeArguments)
        {
            var inferred = InferTypeArguments(type.TypeParameters,
                [.. fieldPerArgument.Value.Select(f => f.Type)],
                [.. arguments.Select(a => a.Type)]);
            if (inferred is null)
                return Fail(TypeApplicability.CannotInferTypeArguments);

            var specializedType = type.Specialize(inferred.Value);
            SideEffects.AddResolvedSymbol(typeSyntax.Location, specializedType);
            
            return new TypeCandidate(specializedType, TypeApplicability.Applicable, SideEffects);
        }

        return new TypeCandidate(type, TypeApplicability.Applicable, SideEffects);
        
        TypeCandidate Fail(TypeApplicability applicability)
        {
            SideEffects.AddResolvedSymbol(typeSyntax.Location, type);
            SideEffects.ReportError(MakeTypeResolutionError([type], applicability, typeSyntax, syntax));
            return new TypeCandidate(type, applicability, SideEffects);
        }
    }

    private Diagnostic.Error? MakeTypeResolutionError(ImmutableArray<TypeSymbol> candidates, TypeApplicability applicability,
        SyntaxNode typeSyntax, CallExprSyntax callSyntax)
        => applicability switch
        {
            TypeApplicability.CannotInitialize => new Diagnostic.CannotInitialize(candidates, typeSyntax),
            
            TypeApplicability.FieldNameMismatch when candidates.Length == 1 => null,    // Individual UndefinedField is reported
            TypeApplicability.FieldNameMismatch  => new Diagnostic.NoMatchingType(candidates, callSyntax.ArgList),
            
            TypeApplicability.CannotInferTypeArguments when candidates.Length == 1 => new Diagnostic.MissingTypeArguments(candidates.CastArray<Symbol>(), typeSyntax),
            TypeApplicability.CannotInferTypeArguments  => new Diagnostic.CannotInferTypeArguments([.. candidates], typeSyntax),
            
            TypeApplicability.Applicable => new Diagnostic.AmbiguousTypeInit(candidates, typeSyntax),
            _ => throw new UnreachableException()
        };

    private ImmutableArray<FieldSymbol>? BindFieldsToArguments(ImmutableArray<BoundArgument> arguments, TypeSymbol type)
    {
        var fields = ImmutableArray.CreateBuilder<FieldSymbol?>();

        foreach (var argument in arguments)
        {
            Debug.Assert(argument.FieldNameSyntax is not null, "Missing field names must be handled before.");

            var fieldName = argument.FieldNameSyntax.Text.ToString();
            var field = type.Members.OfType<FieldSymbol>().FirstOrDefault(field => field.Name == fieldName);

            if (field is null)
                SideEffects.ReportError(new Diagnostic.UndefinedField(type, argument.FieldNameSyntax!));

            fields.Add(field);
        }

        if (fields.Any(f => f is null))
            return null;
        
        return fields.DrainToImmutable()!;
    }

    /// <summary>
    /// Completes to bind a type init. Expected that
    /// (a) all fields are assigned to (they don't need to match types)
    /// (b) all arguments match a unique field
    /// (c) the type is specialized
    /// </summary>
    private BoundValue CompleteTypeInit(TypeCandidate candidate, ImmutableArray<BoundArgument> arguments, CallExprSyntax syntax)
    {
        var fieldsToArguments = BindFieldsToArguments(arguments, candidate.Type);
        Debug.Assert(fieldsToArguments is not null);
        Debug.Assert(fieldsToArguments.Value.Length == arguments.Length);

        var fieldInits = Enumerable.Range(0, arguments.Length)
            .Select(i => BindFieldInitializer(arguments[i], fieldsToArguments.Value[i]))
            .ToImmutableArray();
        if (fieldInits.Any(f => f is not BoundFieldInit))
            return new BoundError(ErrorType, syntax);
        
        return new BoundStructInit(candidate.Type, [.. fieldInits.Cast<BoundFieldInit>()], syntax);
    }
    
    private BoundValue BindFieldInitializer(BoundArgument argument, FieldSymbol field)
    {
        if (!CheckAccessibility(field, argument.FieldNameSyntax!) ||
            !CheckType(argument.Value, field.Type))
        {
            return new BoundError(ErrorType, argument.Syntax!);
        }

        return new BoundFieldInit(field, argument.Value, argument.Syntax);
    }

    #endregion
}