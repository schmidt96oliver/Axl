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

public sealed class Binder
{
    private readonly Scope _scope;
    private readonly DiagnosticBag _diagnostics;
    private readonly SemanticSideTable _semanticSideTable;
    
    #region Comfort Stuff

    public StructSymbol Bool => _scope.BaseNamespace.Bool;
    
    public StructSymbol String => _scope.BaseNamespace.String;
    
    public StructSymbol Unit => _scope.BaseNamespace.Unit;


    public Binder WithOutput(DiagnosticBag diagnosticBag, SemanticSideTable semanticSideTable)
        => new(_scope, diagnosticBag, semanticSideTable);

    public Binder WithScope(Scope scope)
        => new(scope, _diagnostics, _semanticSideTable);
    
    public Binder EnterFunSignature(FunSymbol fun)
        => WithScope(new FunSignatureScope(_scope, fun));

    public Binder EnterFunBody()
    {
        Guard.IsState(_scope.Owner is FunSymbol);
        return WithScope(new FunBodyScope(_scope));
    }

    public Binder EnterTypeHeader(TypeSymbol type)
        => WithScope(new TypeHeaderScope(_scope, type));
    
    public Binder EnterTypeBody()
        => WithScope(new TypeBodyScope(_scope));

    public Binder EnterBlock(ImmutableArray<Symbol> localMembers)
        => WithScope(new BlockScope(_scope, localMembers));

    public Binder WithAdditionalFlags(BindingFlags flags)
        => WithScope(new FlagsScope(_scope, _scope.Flags | flags));
    
    #endregion

    
    private Binder(Scope scope, DiagnosticBag diagnosticBag, SemanticSideTable semanticSideTable)
    {
        _diagnostics = diagnosticBag;
        _semanticSideTable = semanticSideTable;

        _scope = scope;
    }

   
    
    private static bool IsAssignableTo(TypeSymbol source, TypeSymbol target)
    {
        // Errors are silent
        if (source is ErrorTypeSymbol || target is ErrorTypeSymbol) return true;
        
        // Never assigns to anything
        if (source is NeverTypeSymbol) return true;

        return source == target;
    }

    private bool CheckType(BoundValue value, TypeSymbol expected)
    {
        if (!IsAssignableTo(value.Type, expected))
        {
            _diagnostics.ReportError(new Diagnostic.TypeMismatch(
                Value: value,
                Expected: expected));
            return false;
        }

        return true;
    }
    
    private bool CheckAccessibility(Symbol symbol, SyntaxNode syntax)
    {
        if (!_scope.IsAccessible(symbol))
        {
            _diagnostics.ReportError(new Diagnostic.NotAccessible([symbol], syntax));
            return false;
        }

        return true;
    }


    private BoundError BindError(SyntaxNode syntax, Diagnostic.Error error)
    {
        _diagnostics.ReportError(error);
        return new BoundError(syntax);
    }
    

    #region Lookup

    private enum LookupOptions
    {
        Any,
        OnlyType,
        MustBeCallable,
    }
    
    private LookupViability GetViability(Symbol symbol, LookupOptions options)
    {
        if (!_scope.IsAccessible(symbol)) 
            return LookupViability.NotAccessible;

        if (options is LookupOptions.OnlyType && symbol is not TypeSymbol)
            return LookupViability.WrongKind;
        if (options is LookupOptions.MustBeCallable && symbol is not (TypeSymbol or FunSymbol))
            return LookupViability.WrongKind;

        return LookupViability.Viable;
    }

    private LookupResult LookupName(string name, LookupOptions options)
    {
        var result = new LookupResult();

        for (var scope = _scope; scope != null; scope = scope.EnclosingScope)
        {
            var here = new LookupResult();
            foreach (var s in scope.Lookup(name))
            {
                Debug.Assert(s.Name == name);
                here.MergeSingle(s, GetViability(s, options));
            }

            result.MergeWith(here);

            if (result.Viability == LookupViability.Viable)
                break;
        }

        Debug.Assert(result.Symbols.All(s => s.Name == name));
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
    
    
    #region File and Script

    public static BoundFile BindFile(FileSyntax syntax)
    {
        var diagnostics = new DiagnosticBag();
        var semanticSideTable = new SemanticSideTable();
        
        var rootNamespace = new RootNamespaceSymbol();
        var rootBinder = new Binder(new RootScope(rootNamespace), diagnostics, semanticSideTable);
        
        var scriptFun = new FunSymbol(name: "", 
            isOperator: false,
            owner: rootNamespace,
            isPublic: false,
            receiverType: null,
            isMutatingReceiver: false);

        scriptFun.SetParametersAndReturnType([], rootBinder.Unit);
        scriptFun.SetBody(rootBinder.EnterFunSignature(scriptFun).BindScriptBody(syntax, out var localTypes));
        
        return new BoundFile(scriptFun, localTypes, diagnostics.Drain(), semanticSideTable);
    }

    private BoundBlock BindScriptBody(FileSyntax syntax, out ImmutableArray<TypeSymbol> localTypes)
    {
        var localMembers = BindScriptLocalTypesAndFuns(syntax, out var localFuns, out localTypes);
        
        var stmts = EnterBlock(localMembers).BindStmtsAndLocalFunBodies(
            [.. syntax.SyntaxNodes().Where(node => node is not StructDeclSyntax)], localFuns);
        var block = new BoundBlock(stmts, localFuns, Unit, syntax);

        // Insert unit return if necessary
        if (!block.IsDiverging)
            block = block.WithAdditionalStmts(new BoundReturn(null));

        return block;
    }
    
    private ImmutableArray<TypeSymbol> BindScriptLocalTypes(FileSyntax syntax)
    {
        // Bind structs fully on their own block that will
        // be thrown away, so they can't see what else is 
        // on the script scope.
        
        // Because of dependencies, bind first all symbols, then all members,
        // then all bodies.
        var structs = syntax.Children.OfType<StructDeclSyntax>().Select(BindStructSymbol).ToImmutableArray();

        var scriptBodyBinder = EnterBlock(structs.CastArray<Symbol>());
        foreach (var @struct in structs)
            @struct.SetMembers(scriptBodyBinder.EnterTypeHeader(@struct).BindStructMembers(@struct.DeclarationSyntax!));
        foreach (var @struct in structs)
            scriptBodyBinder.EnterTypeHeader(@struct).BindStructBody(@struct);

        return structs.CastArray<TypeSymbol>();
    }

    private ImmutableArray<Symbol> BindScriptLocalTypesAndFuns(FileSyntax syntax,
        out ImmutableArray<FunSymbol> localFuns, out ImmutableArray<TypeSymbol> localTypes)
    {
        // Types are standalone inside a script, they don't see any locals.
        // Local funs signatures must see those types, but don't need to see
        // each other. Finally, stmts are bound on the _same_ block as types
        // and local funs are visibile on, because variable declaration will
        // test against that (CannotShadow local fun / local type).

        localTypes = BindScriptLocalTypes(syntax);

        var funSignatureBinder = EnterBlock(localTypes.CastArray<Symbol>());

        localFuns =
        [
            .. syntax.Children.OfType<FunDeclSyntax>()
                .Select(funSignatureBinder.BindFunSymbol)
        ];

        ImmutableArray<Symbol> localMembers = [.. localTypes, .. localFuns];

        CheckDuplicateDeclarations(localMembers);
        return localMembers;
    }
    
    #endregion
    
    #region Funs and Structs

    private FunSymbol BindFunSymbol(FunDeclSyntax syntax)
    {
        Guard.IsState(_scope.OwnerAsFun is not null || _scope.OwnerAsType is not null, $"{nameof(BindFunSymbol)} only valid on types or funs.");

        var funSymbol = _scope.OwnerAsFun is not null
            ? BindLocalFunSymbol(syntax)
            : BindTypeFunSymbol(syntax);
        
        // Bind signature
        var signatureBinder = EnterFunSignature(funSymbol);
        var returnType = syntax.ReturnTypeAnnotation is not null
            ? signatureBinder.BindType(syntax.ReturnTypeAnnotation)
            : Unit;
        var parameters = signatureBinder.BindParameters(syntax.ParameterList);
        funSymbol.SetParametersAndReturnType(parameters, returnType);
        
        if (funSymbol.IsOperator)
            CheckOperatorSignature(funSymbol, syntax);

        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, funSymbol);
        return funSymbol;
    }

    private FunSymbol BindLocalFunSymbol(FunDeclSyntax syntax)
    {
        Debug.Assert(_scope.OwnerAsFun is not null);

        // 'Pub' and 'static' are not allowed
        if (syntax.PubKw is not null)
            _diagnostics.ReportError(new Diagnostic.InvalidLocalFunModifier(syntax.PubKw));

        if (syntax.StaticKw is not null)
            _diagnostics.ReportError(new Diagnostic.InvalidLocalFunModifier(syntax.StaticKw));

        // Operators are not allowed.
        // They can be declared though, since they are not callable anyway.
        if (syntax.IsOperatorName)
            _diagnostics.ReportError(new Diagnostic.OperatorOutsideType(syntax.Name));
        
        var receiverType = _scope.OwnerAsFun.ReceiverType;
        var isMutatingReceiver = false;

        if (syntax.VarKw is not null)
        {
            // Var on local funs is only allowed, if its parent is var.
            if (receiverType is null)
                _diagnostics.ReportError(new Diagnostic.VarLocalFunInStaticContext(syntax.VarKw));
            else if (!_scope.OwnerAsFun!.IsMutatingReceiver)
                _diagnostics.ReportError(new Diagnostic.VarLocalFunInNonVarFun(syntax.VarKw));
            else
                isMutatingReceiver = true;
        }

        return new FunSymbol(syntax.Name.Text.ToString(),
            isOperator: false,
            owner: _scope.Owner,
            isPublic: false,
            receiverType,
            isMutatingReceiver,
            declarationSyntax: syntax);
    }

    private FunSymbol BindTypeFunSymbol(FunDeclSyntax syntax)
    {
        Debug.Assert(_scope.OwnerAsType is not null);
        
        var name = syntax.Name.Text.ToString();
        
        var isPublic = syntax.PubKw is not null;
        var receiverType = syntax.StaticKw is not null ? null : _scope.OwnerAsType;
        var isMutatingReceiver = syntax.VarKw is not null;

        if (receiverType is null && isMutatingReceiver)
        {
            _diagnostics.ReportError(new Diagnostic.StaticFunCannotBeVar(syntax.VarKw!, name));
            isMutatingReceiver = false;
        }
        
        if (syntax.IsOperatorName && receiverType is null)
            _diagnostics.ReportError(new Diagnostic.OperatorCannotBeStatic(name, syntax.StaticKw!));
        if (syntax.IsOperatorName && isMutatingReceiver)
            _diagnostics.ReportError(new Diagnostic.OperatorCannotBeVar(name, syntax.VarKw!));

        var funSymbol = new FunSymbol(name,
            isOperator: syntax.IsOperatorName,
            owner: _scope.Owner,
            isPublic,
            receiverType,
            isMutatingReceiver,
            declarationSyntax: syntax);
        
        return funSymbol;
    }

    private void CheckOperatorSignature(FunSymbol funSymbol, FunDeclSyntax syntax)
    {
        var opToken = syntax.Name.Token;
        var isUnary = SyntaxFacts.IsUnaryOperator(opToken.Kind);
        var isBinary = SyntaxFacts.IsBinaryOperator(opToken.Kind);
        Debug.Assert(isUnary || isBinary);

        // Declared operator must match unary/binary
        var isParamCountValid = (isUnary, isBinary, funSymbol.Parameters.Length) is
            (true, true, 0 or 1) or (true, false, 0) or (false, true, 1);
        
        if (!isParamCountValid)
            _diagnostics.ReportError(new Diagnostic.WrongOperatorParameterCount(funSymbol));
        
        // Comparison operators must return bool
        if (SyntaxFacts.IsComparisonOperator(opToken.Kind) && 
            funSymbol.ReturnType != Bool && funSymbol.ReturnType is not ErrorTypeSymbol)
        {
            _diagnostics.ReportError(new Diagnostic.WrongOperatorReturnType(funSymbol, Bool));
        }
    }

    private BoundBlock BindFunBody(FunBodySyntax syntax)
    {
        if (_scope.OwnerAsFun is not FunSymbol fun)
            throw new InvalidOperationException($"{nameof(BindFunBody)} only valid on funs.");

        var bodyBinder = EnterFunBody();

        // --- Empty body
        if (syntax.Expr is null)
            throw new NotImplementedException("Funs without body not supported yet.");

        // --- Expression body
        if (syntax.IsExpressionBodied)
        {
            var expr = bodyBinder.BindValue(syntax.Expr);
            if (!CheckType(expr, fun.ReturnType))
                expr = new BoundError(syntax.Expr);

            return new BoundBlock([new BoundReturn(expr)], [], Unit);
        }

        // --- Block body
        Debug.Assert(syntax.Expr is BlockExprSyntax);
        var block = bodyBinder.BindBlock((BlockExprSyntax)syntax.Expr);

        // Check that all code-paths return a value
        if (!block.IsDiverging)
        {
            if (fun.ReturnType == Unit)
                block = block.WithAdditionalStmts(new BoundReturn(null));
            else
                _diagnostics.ReportError(new Diagnostic.MissingReturn(block.Syntax!));
        }

        return block;
    }
    
    private ImmutableArray<ParameterSymbol> BindParameters(ParamListSyntax syntax)
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
            _diagnostics.ReportError(new Diagnostic.DuplicateParameters(duplicateGroup));

        return parameters;
    }
    
    private ParameterSymbol BindParameter(ParamSyntax syntax)
    {
        if (_scope.OwnerAsFun is null)
            throw new InvalidOperationException($"{nameof(BindParameter)} only valid on funs.");

        var type = BindType(syntax.TypeAnnotation);
        var symbol = new ParameterSymbol(syntax.Name.Identifier, _scope.OwnerAsFun!, type, syntax);
        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }

    private void BindStructBody(StructSymbol symbol)
    {
        CheckRecursiveStructLayout(symbol);
        
        var typeBinder = EnterTypeBody();

        var funs = symbol.Members.OfType<FunSymbol>();

        foreach (var fun in funs)
        {
            Debug.Assert(fun.DeclarationSyntax is not null);
            fun.SetBody(typeBinder.EnterFunSignature(fun).BindFunBody(fun.DeclarationSyntax.Body));
        }
    }

    private StructSymbol BindStructSymbol(StructDeclSyntax syntax)
    {
        var symbol = new StructSymbol(syntax.Name.Identifier,
            owner: _scope.Owner, isPublic: false, isPrimitive: false, syntax);
        if (symbol.Name.Length > 0)
            _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        return symbol;
    }

    private ImmutableArray<Symbol> BindStructMembers(StructDeclSyntax syntax)
    {
        var fields = syntax.Body?.Children
            .OfType<FieldDeclSyntax>()
            .Select(Symbol (fieldSyntax) => BindFieldSymbol(fieldSyntax))
            .ToImmutableArray() ?? [];
        var funs = syntax.Body?.Children
            .OfType<FunDeclSyntax>()
            .Select(BindFunSymbol)
            .ToImmutableArray() ?? [];

        ImmutableArray<Symbol> members = [.. fields, .. funs];
        CheckDuplicateDeclarations(members);
        return members;
    }

    private FieldSymbol BindFieldSymbol(FieldDeclSyntax syntax)
    {
        if (_scope.Owner is not StructSymbol structSymbol)
            throw new InvalidOperationException("Can only bind fields inside structs.");
        
        var isPub = syntax.PubKw is not null;
        var type = BindType(syntax.TypeExpr);
        var symbol = new FieldSymbol(syntax.Name.Token.Identifier, structSymbol, isPub, type, syntax);
        if (symbol.Name.Length > 0)
            _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, symbol);
        
        return symbol;
    }


    private void CheckRecursiveStructLayout(StructSymbol symbol)
    {
        Recurse(symbol, []);
        return;
        
        void Recurse(StructSymbol current, FieldSymbol[] path)
        {
            foreach (var field in current.Members.OfType<FieldSymbol>())
            {
                if (field.Type is not StructSymbol fieldStructType)
                    continue;

                if (fieldStructType == symbol)
                {
                    _diagnostics.ReportError(new Diagnostic.RecursiveStructLayout(symbol, [.. path, field]));
                }
                else
                    Recurse(fieldStructType, [.. path, field]);
            }
        }
    }
    
    private void CheckDuplicateDeclarations(ImmutableArray<Symbol> declared)
    {
        var symbolsByName = declared
            .Where(symbol => symbol.Name is not "")
            .GroupBy(symbol => symbol.Name)
            .Select(duplicates => duplicates.ToImmutableArray())
            .ToImmutableArray();

        foreach (var duplicateArray in symbolsByName.Where(duplicates => duplicates.Length > 1))
        {
            // For multiple funs, we still need to check their signatures.
            // If anything non-fun is on the duplicate list, we know its an error.
            if (duplicateArray.Any(s => s is not FunSymbol))
            {
                _diagnostics.ReportError(new Diagnostic.AlreadyDeclared(duplicateArray));
                continue;
            }
            
            // Now it is all funs and we need to check the signatures
            CheckDuplicateDeclarationsInFunGroup([.. duplicateArray.Select(s => (FunSymbol)s)]);
        }
    }

    private void CheckDuplicateDeclarationsInFunGroup(IEnumerable<FunSymbol> funsWithSameName)
    {
        var remaining = funsWithSameName.ToList();
        while (remaining.Count > 0)
        {
            var first = remaining[0];
            var sameSignature = remaining
                .Where(fun => fun.ParameterTypes.SequenceEqual(first.ParameterTypes))
                .ToImmutableArray();
            if (sameSignature.Length > 1)
            {
                // Errors in the signature should not report another error.
                var duplicatesToReport = sameSignature
                    .Where(fun => fun.ParameterTypes.All(paramType => paramType is not ErrorTypeSymbol))
                    .ToImmutableArray();

                if (duplicatesToReport.Length > 1)
                    _diagnostics.ReportError(new Diagnostic.DuplicateFunDeclarations(sameSignature));
            }

            remaining.RemoveAll(sameSignature.Contains);
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

        return BindValue(exprSyntax);
    }
    
    private BoundStmt BindVarDecl(VarDeclSyntax syntax)
    {
        var variableType = syntax.TypeAnnotation is not null
            ? BindType(syntax.TypeAnnotation)
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
        var lookupResult = _scope.Lookup(syntax.Name.Identifier).FirstOrDefault();
        if (lookupResult is not (null or VariableSymbol))
            return BindError(syntax, new Diagnostic.CannotShadow(lookupResult, syntax.Name));
        
        var isReadOnly = syntax.VarOrLetKwToken.Kind is TokenKind.LetKw;
        var variable = new VariableSymbol(
            syntax.Name.Identifier, 
            _scope.OwnerAsFun ?? throw new InvalidOperationException("Values can only be bound on fun bodies."),
            isReadOnly, 
            variableType);
        _semanticSideTable.AddResolvedSymbol(syntax.Name.Location, variable);
    
        return new BoundVarDecl(variable, boundInitializer, syntax);
    }
    
    private BoundValue BindVarDeclInitializer(VarDeclSyntax syntax)
    {
        if (syntax.Initializer is null)
            return BindError(syntax, new Diagnostic.MissingInitializer(syntax));
    
        return BindValue(syntax.Initializer);
    }
    
    private BoundStmt BindWhile(WhileStmtSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);
        var body = WithAdditionalFlags(BindingFlags.InsideLoop).BindValue(syntax.Body);
        
        return new BoundWhile(condition, body, syntax);
    }
    
    #endregion

    
    private BoundNode BindExpr(ExprSyntax syntax, LookupOptions lookupOptions) => syntax switch
    {
        MemberAccessExprSyntax memberAccessExprSyntax => BindMemberAccess(memberAccessExprSyntax),
        IdNameSyntax idNameSyntax => BindIdName(idNameSyntax, lookupOptions),
        SelfSyntax selfSyntax => BindSelf(selfSyntax),
        CallExprSyntax callExprSyntax => BindCall(callExprSyntax),
        
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
        GroupExprSyntax groupExprSyntax => BindValue(groupExprSyntax.Inner),
        BreakExprSyntax breakExprSyntax => BindBreakOrContinue(breakExprSyntax),
        ContinueExprSyntax or BreakExprSyntax => BindBreakOrContinue(syntax),
        ReturnExprSyntax returnExprSyntax => BindReturn(returnExprSyntax),
        
        // Error
        ErrorExprSyntax errorExprSyntax => new BoundError(errorExprSyntax),
        FunNameSyntax => throw new UnreachableException(),
    };

    private BoundValue BindValue(ExprSyntax syntax)
    {
        var bound = BindExpr(syntax, LookupOptions.Any);
        if (bound is BoundValueOrTypeRef valueOrType)
            return valueOrType.AsValue;
        if (bound is not BoundValue value)
            return BindError(syntax, new Diagnostic.NotAValue(bound));

        return value;
    }

    private TypeSymbol BindType(ExprSyntax syntax)
    {
        var bound = BindExpr(syntax, LookupOptions.OnlyType);
        if (bound is BoundValueOrTypeRef valueOrType)
            return valueOrType.AsTypeRef.Type;
        if (bound is not BoundTypeRef typeRef)
        {
            if (bound is not BoundError)
                _diagnostics.ReportError(new Diagnostic.NotAType(bound));
            return ErrorTypeSymbol.Instance;
        }

        return typeRef.Type;
    }
    
    
    #region Literals and Strings and Self

    private BoundNode BindSelf(SelfSyntax syntax)
    {
        if (_scope.InstanceType is null)
            return BindError(syntax, new Diagnostic.InvalidSelfRef(syntax));

        return new BoundSelf(_scope.InstanceType, _scope.OwnerAsFun!.IsMutatingReceiver, syntax);
    }

    private BoundValue BindNumberLiteral(NumberLiteralSyntax syntax)
    {
        var type = syntax.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => _scope.BaseNamespace.I32,
            NumberLiteralSuffix.I64 => _scope.BaseNamespace.I64,
            NumberLiteralSuffix.F32 => _scope.BaseNamespace.F32,
            NumberLiteralSuffix.F64 => _scope.BaseNamespace.F64,

            _ => syntax.Token.HasDecimalPoint ? _scope.BaseNamespace.DefaultFloatType : _scope.BaseNamespace.DefaultIntType
        };

        // Literals like "1.1i32" need to be rejected.
        if (syntax.Token.HasDecimalPoint &&
            type != _scope.BaseNamespace.F32 && type != _scope.BaseNamespace.F64)
        {
            return BindError(syntax, new Diagnostic.SuffixInvalidForDecimalNumber(syntax));
        }

        if (type == _scope.BaseNamespace.I32)
        {
            if (!int.TryParse(syntax.Token.Body, out var value))
                return BindError(syntax, new Diagnostic.NumberTooBig(type, syntax));

            return new BoundConst(value, type, syntax);
        }

        if (type == _scope.BaseNamespace.I64)
        {
            if (!long.TryParse(syntax.Token.Body, out var value))
                return BindError(syntax, new Diagnostic.NumberTooBig(type, syntax));

            return new BoundConst(value, type, syntax);
        }

        if (type == _scope.BaseNamespace.F32)
        {
            var value = float.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }

        if (type == _scope.BaseNamespace.F64)
        {
            var value = double.Parse(syntax.Token.Body, CultureInfo.InvariantCulture);
            return new BoundConst(value, type, syntax);
        }

        throw new UnreachableException();
    }
    
    
    private BoundStringExpr BindString(StringExprSyntax syntax)
    {
        var parts = syntax.Parts.Select(BindStringPart).ToImmutableArray();
    
        if (parts.Length == 0)
            parts = [new BoundConst("", String, syntax)];
        
        return new BoundStringExpr(parts, String, syntax);
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
                
        var boundExpr = BindValue(syntax.Expr);

        if (IsAssignableTo(boundExpr.Type, String))
            return boundExpr;

        if (TryBindMethodCall(boundExpr, "ToString", [], syntax) is not BoundCall boundCall
            || boundCall.Type != String)
        {
            return BindError(syntax, new Diagnostic.CannotConvertToString(boundExpr));
        }

        return boundCall;
    }
    
    #endregion

    #region Operators and Assignment

    private BoundValue BindUnary(UnaryExprSyntax syntax)
        => BindSynthesizedOperatorCall(BindValue(syntax.Operand), syntax.Operator, [], syntax);
    
    private BoundValue BindBinary(BinaryExprSyntax syntax)
    {
        if (syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar)
            return BindLogicOperator(syntax);
        if (syntax.Operator.Kind == TokenKind.Equal)
            return BindAssign(syntax);
        
        var left = BindValue(syntax.Left);
        var right = BindValue(syntax.Right);

        return BindSynthesizedOperatorCall(left, syntax.Operator, [right], syntax);
    }

    private BoundValue BindSynthesizedOperatorCall(BoundValue receiver, Token @operator,
        ImmutableArray<BoundValue> arguments, SyntaxNode syntax)
    {
        if (TryBindMethodCall(receiver, @operator.Text.ToString(), arguments, syntax) is not BoundCall boundCall)
        {
            // If any operand had an error, stay silent.
            if (receiver.Type is ErrorTypeSymbol || arguments.Any(arg => arg.Type is ErrorTypeSymbol))
                return new BoundError(syntax);
            
            return BindError(syntax, new Diagnostic.UndefinedOperator(
                @operator,
                [receiver.Type, .. arguments.Select(arg => arg.Type)]));
        }

        _semanticSideTable.AddResolvedSymbol(@operator.Location, boundCall.Fun);
        return boundCall;
    }

    private BoundValue BindLogicOperator(BinaryExprSyntax syntax)
    {
        Debug.Assert(syntax.Operator.Kind is TokenKind.DoubleAmpersand or TokenKind.DoubleVerticalBar);
    
        var left = BindValue(syntax.Left);
        var right = BindValue(syntax.Right);
        if (left.Type is ErrorTypeSymbol || right.Type is ErrorTypeSymbol)
        {
            // Some operands have an error. So don't type-check them
            // be silent and wrap in an error expression.
            return new BoundError(syntax);
        }
    
        // Type-check against bool
        if (!CheckType(left, Bool) ||
            !CheckType(right, Bool))
        {
            return new BoundError(syntax);
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

        var target = BindValue(syntax.Left);
        var value = BindValue(syntax.Right);

        if (target is BoundError)
            return new BoundError(syntax);
        if (value.Type is ErrorTypeSymbol)
            return new BoundAssign(target, value, ErrorTypeSymbol.Instance, syntax);

        if (!target.IsPlace || !target.IsAssignable)
            return BindError(syntax, new Diagnostic.CannotAssign(target));

        TypeSymbol type = CheckType(value, target.Type)
            ? Unit
            : ErrorTypeSymbol.Instance;
        return new BoundAssign(target, value, type, syntax);
    }
    
    #endregion
    
    #region Blocks, Control Flow
    
    private BoundBlock BindBlock(BlockExprSyntax syntax)
    {
        var localFuns = syntax.Children.OfType<FunDeclSyntax>().Select(BindFunSymbol).ToImmutableArray();
        CheckDuplicateDeclarations([.. localFuns]);

        var inBlockBinder = EnterBlock([.. localFuns]);
        
        var stmts = inBlockBinder.BindStmtsAndLocalFunBodies([.. syntax.SyntaxNodes()], localFuns);
        return new BoundBlock(stmts, localFuns, Unit, syntax);
    }

    private ImmutableArray<BoundStmt> BindStmtsAndLocalFunBodies(ImmutableArray<SyntaxNode> childNodes, ImmutableArray<FunSymbol> localFuns)
    {
        if (_scope is not BlockScope blockScope)
            throw new InvalidOperationException("Must be on block scope.");

        var stmts = ImmutableArray.CreateBuilder<BoundStmt>();
        for (var i = 0; i < childNodes.Length; i++)
        {
            var node = childNodes[i];
            switch (node)
            {
                case StmtSyntax stmtSyntax:
                    var boundStmt = BindStmt(stmtSyntax);
                    stmts.Add(boundStmt);
                    if (boundStmt is BoundVarDecl boundVarDecl)
                    {
                        // Create an adjacent scope and continue from there.
                        if (i + 1 < childNodes.Length)
                        {
                            var adjacentBinder = WithScope(blockScope.WithVariable(boundVarDecl.Variable));
                            stmts.AddRange(adjacentBinder.BindStmtsAndLocalFunBodies(childNodes[(i + 1)..], localFuns));
                        }
                        
                        return stmts.DrainToImmutable();
                    }
                    break;

                case FunDeclSyntax funDeclSyntax:
                    var localFunSymbol = localFuns.Single(fun => fun.DeclarationSyntax == funDeclSyntax);
                    var localFunBinder = EnterFunSignature(localFunSymbol);
                    localFunSymbol.SetBody(localFunBinder.BindFunBody(funDeclSyntax.Body));
                    break;

                case StructDeclSyntax:
                    throw new UnreachableException("Structs are handled in script/global scope binding.");

                default:
                    _diagnostics.ReportError(new Diagnostic.UnsupportedFeature(node));
                    break;
            }
        }

        return stmts.DrainToImmutable();
    }

    private BoundValue BindCondition(ExprSyntax syntax)
    {
        var condition = BindValue(syntax);
        if (!CheckType(condition, expected: Bool))
            condition = new BoundError(syntax);

        return condition;
    }
    
    private BoundValue BindIfExpr(IfExprSyntax syntax)
    {
        var condition = BindCondition(syntax.Condition);
        var body = BindValue(syntax.Body);
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
            _diagnostics.ReportError(new Diagnostic.IncompatibleBranches(body, @else));
            type = ErrorTypeSymbol.Instance;
        }
        
        return new BoundIfExpr(condition, body, @else, type, syntax);
    }

    private BoundValue BindElseExpr(IfExprSyntax ifSyntax)
    {
        var elseSyntax = ifSyntax.ElseBody;
        if (elseSyntax is null)
            return BindError(ifSyntax, new Diagnostic.MissingElse(ifSyntax));

        return BindValue(elseSyntax);
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
        
        if (!_scope.IsInsideLoop)
            return BindError(syntax, new Diagnostic.BreakOrContinueOutsideLoop(syntax));
    
        return syntax is BreakExprSyntax
            ? new BoundBreak(syntax)
            : new BoundContinue(syntax);
    }

    private BoundValue BindReturn(ReturnExprSyntax syntax)
    {
        if (_scope.OwnerAsFun is not FunSymbol fun)
            throw new InvalidOperationException("Values can only be bound on fun bodies.");
        
        var expr = syntax.Expr is not null ? BindValue(syntax.Expr) : null;

        if (expr is not null)
        {
            if (!CheckType(expr, fun.ReturnType))
                expr = new BoundError(syntax.Expr);
        }
        else if (fun.ReturnType != Unit)
        {
            _diagnostics.ReportError(new Diagnostic.MissingReturnValue(fun, syntax));
            expr = new BoundError();
        }

        return new BoundReturn(expr, syntax);
    }
    
    #endregion

    #region Names and Member access
    
    private BoundNode BindIdName(IdNameSyntax syntax, LookupOptions lookupOptions)
    {
        if (syntax.Token.IsMissing)
            return new BoundError(syntax);

        var lookupResult = LookupName(syntax.Token.Identifier, lookupOptions);

        if (lookupResult.Viability is LookupViability.NotFound)
            return BindError(syntax, new Diagnostic.UndefinedName(syntax));

        if (lookupResult.Viability is LookupViability.NotAccessible)
            return BindError(syntax, new Diagnostic.NotAccessible(lookupResult.Symbols, syntax));

        var symbol = lookupResult.Symbols[0];
        if (symbol is FunSymbol)
            return BindFunGroup(lookupResult, SynthesizeFunGroupReceiver(), syntax);
        
        if (lookupResult.Symbols.Length > 1)
            return BindError(syntax, new Diagnostic.AmbiguousReference(lookupResult, syntax));

        var boundSymbol = symbol switch
        {
            NamespaceSymbol @namespace => new BoundNamespaceRef(@namespace, syntax),
            TypeSymbol type => new BoundTypeRef(type, syntax),
            
            VariableSymbol variable => BindVariable(variable, syntax),
            FunSymbol => BindFunGroup(lookupResult, SynthesizeFunGroupReceiver(), syntax),
            FieldSymbol field => BindFieldAccess(field, null, syntax)
        };

        // Note that the symbol that was bound might differ from the symbol
        // we found, because e.g. field access might do another lookup for
        // a type of the same name.
        if (boundSymbol is IBoundSymbolRef symbolRef)
            _semanticSideTable.AddResolvedSymbol(syntax.Location, symbolRef.Symbol);
            
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
            _ => throw new UnreachableException($"{nameof(LookupOptions)}.{lookupOptions} is unknown to {nameof(BindIdNameWithWrongKind)}")
        };
    

    private BoundNode BindMemberAccess(MemberAccessExprSyntax syntax)
    {
        var left = BindExpr(syntax.Left, LookupOptions.Any);

        if (syntax.Member.Token.IsMissing)
            return new BoundError(syntax.Member);
        
        // A fun group cannot have members
        if (left is BoundFunGroup leftAsFunGroup)
            return BindError(syntax, new Diagnostic.FunHasNoMembers(leftAsFunGroup, syntax.Member));
        
        NamespaceOrTypeSymbol namespaceOrType = left switch
        {
            // Note that BoundValueOrTypeRef goes through BoundValue here
            // and returns it's type.
            BoundValue value => value.Type,
            
            BoundNamespaceRef boundNamespaceRef => boundNamespaceRef.Namespace,
            BoundTypeRef boundTypeRef => boundTypeRef.Type,
            
            BoundFunGroup => throw new UnreachableException("Fun group handled above."),
            BoundStmt => throw new UnreachableException("Stmt can never be the left side of member access."),
        };
        if (namespaceOrType is ErrorTypeSymbol)
            return new BoundError(syntax.Member);

        var memberName = syntax.Member.Text.ToString();
        var lookupResult = LookupMembers(namespaceOrType, memberName);

        if (lookupResult.Viability is LookupViability.NotFound)
            return BindError(syntax, new Diagnostic.UndefinedMember(left, syntax.Member));

        if (lookupResult.Viability is LookupViability.NotAccessible)
            return BindError(syntax, new Diagnostic.NotAccessible(lookupResult.Symbols, syntax.Member));
        
        var symbol = lookupResult.Symbols[0];
        if (symbol is FunSymbol)
            return BindFunGroup(lookupResult, receiver: left, syntax.Member);

        if (lookupResult.Symbols.Length > 1)
            return BindError(syntax, new Diagnostic.AmbiguousReference(lookupResult, syntax.Member));
        
        var receiver = ReplaceBoundValueOrTypeRef(left, symbol);
        _semanticSideTable.AddResolvedSymbol(syntax.Location, symbol);
        
        return symbol switch
        {
            NamespaceSymbol @namespace => new BoundNamespaceRef(@namespace, syntax.Member),
            TypeSymbol type => new BoundTypeRef(type, syntax.Member),

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
            _semanticSideTable.AddResolvedSymbol(replaced.Syntax!.Location, symbolRef.Symbol);

        return replaced;
    }
    
    private BoundSelf? SynthesizeFunGroupReceiver()
    {
        if (_scope.InstanceType is null)
            return null;
        return new BoundSelf(_scope.InstanceType, mutable: _scope.OwnerAsFun!.IsMutatingReceiver);
    }


    private BoundNode BindVariable(VariableSymbol variable, IdNameSyntax syntax)
    {
        // Variables could be captured from outside, which is forbidden.

        if (variable.Owner != _scope.Owner)
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
            if (typeLookup.SingleViable is TypeSymbol type)
                asTypeRef = new BoundTypeRef(type, syntax);
        }

        if (receiver is null)
        {
            // Field was bound as a plain name.
            // It can be interpreted as type (if possible) or with an implicit self.
            if (_scope.InstanceType is null)
            {
                return asTypeRef is null
                    ? BindError(syntax, new Diagnostic.InstanceMemberInStaticContext(field, _scope.Owner, syntax))
                    : asTypeRef;
            }

            if (_scope.InstanceType != field.OwningType)
            {
                return asTypeRef is null
                    ? BindError(syntax, new Diagnostic.MemberOfEnclosingType(field, _scope.InstanceType, syntax))
                    : asTypeRef;
            }

            receiver = new BoundSelf(_scope.InstanceType, mutable: _scope.OwnerAsFun!.IsMutatingReceiver);    
        }

        if (receiver is not BoundValue valueReceiver)
            return BindError(syntax, new Diagnostic.InstanceMemberThroughType(field, syntax));

        var asField = new BoundField(valueReceiver, field, syntax);
        return asTypeRef is not null ? new BoundValueOrTypeRef(asField, asTypeRef) : asField;
    }

    #endregion
    
    
    #region Call and Struct Initializer

    private enum FunViability
    {
        NotFound,
        ArityMismatch,
        ArgumentTypeMismatch,
        Viable
    }

    /// <summary>
    /// Resolves multiple funs inside a <see cref="BoundFunGroup"/> to one
    /// single <see cref="FunSymbol"/>. If resolution is not possible, reports
    /// the appropriate error and returns <c>null</c>. If a fun is returned,
    /// it has the correct arity and matching argument types.
    /// </summary>
    private FunSymbol? BindSingleFunInGroup(BoundFunGroup funGroup, 
        ImmutableArray<BoundValue> arguments, 
        SyntaxNode argListSyntax)
    {
        var argumentTypes = arguments.Select(arg => arg.Type).ToImmutableArray();
        
        var candidates = new List<FunSymbol>();
        var viability = FunViability.NotFound;

        foreach (var fun in funGroup.Candidates)
        {
            var thisFunViability = CheckFunCandidate(fun, argumentTypes);
            if (thisFunViability == viability)
                candidates.Add(fun);
            if (thisFunViability > viability)
            {
                viability = thisFunViability;
                candidates.Clear();
                candidates.Add(fun);
            }
        }
        
        Debug.Assert(viability > FunViability.NotFound);
        Debug.Assert(candidates.Count >= 1);

        if (viability is FunViability.ArityMismatch)
        {
            _diagnostics.ReportError(new Diagnostic.ArityMismatch(
                [.. candidates],
                arguments.Length,
                argListSyntax));
            return null;
        }

        // If any argument had an error, we can only report ArityMismatch
        // and the rest should be silent, since we can't resolve with erroneous
        // arguments.
        if (argumentTypes.OfType<ErrorTypeSymbol>().Any())
            return null;
        
        if (viability is FunViability.ArgumentTypeMismatch)
        {
            // If only one candidate was left, report on each argument
            // individually.

            if (candidates.Count > 1)
            {
                _diagnostics.ReportError(new Diagnostic.NoMatchingOverload(
                    [.. candidates],
                    argumentTypes,
                    argListSyntax));
                return null;
            }

            var candidate = candidates[0];
            for (var i = 0; i < arguments.Length; i++)
            {
                if (!IsAssignableTo(arguments[i].Type, candidate.ParameterTypes[i]))
                {
                    _diagnostics.ReportError(new Diagnostic.ArgumentTypeMismatch(
                        arguments[i],
                        candidate.Parameters[i]));
                }
            }

            return null;
        }
        
        Debug.Assert(viability is FunViability.Viable);

        if (candidates.Count > 1)
        {
            _diagnostics.ReportError(new Diagnostic.AmbiguousCall([.. candidates], funGroup.Syntax!));
            return null;
        }

        if (funGroup.Syntax is not null)
            _semanticSideTable.AddResolvedSymbol(funGroup.Syntax.Location, candidates[0]);
        return candidates[0];
    }

    private FunViability CheckFunCandidate(FunSymbol fun, ImmutableArray<TypeSymbol> argumentTypes)
    {
        if (fun.Parameters.Length != argumentTypes.Length)
            return FunViability.ArityMismatch;

        if (!argumentTypes.Zip(fun.ParameterTypes, IsAssignableTo).All(x => x))
            return FunViability.ArgumentTypeMismatch;

        return FunViability.Viable;
    }


    /// <summary>
    /// Checks that a fun can be called. Expects, that arity and argument types are
    /// already valid.
    /// </summary>
    private bool CheckCallToSingleFun(FunSymbol fun, 
        BoundNode? receiver,
        ImmutableArray<BoundValue> arguments, 
        SyntaxNode funSyntax)
    {
        Debug.Assert(fun.ParameterTypes
            .Zip(arguments.Select(arg => arg.Type), (param, arg) => IsAssignableTo(arg, param))
            .All(x => x));
        
        // Check StaticMismatch
        var isContextStatic = receiver is (null or not BoundValue);
        if (isContextStatic != fun.IsStatic)
        {
            _diagnostics.ReportError(receiver switch
            {
                null => new Diagnostic.InstanceMemberInStaticContext(
                    fun,
                    _scope.Owner,
                    funSyntax),
                not BoundValue => new Diagnostic.InstanceMemberThroughType(
                    fun,
                    funSyntax),
                BoundValue => new Diagnostic.StaticMemberThroughInstance(
                    fun,
                    funSyntax)
            });
            return false;
        }

        // Check ReceiverMismatch and mutation on methods
        if (!isContextStatic)
        {
            Debug.Assert(receiver is BoundValue);
            var valueReceiver = (receiver as BoundValue)!;

            if (fun.ReceiverType != valueReceiver.Type)
            {
                _diagnostics.ReportError(new Diagnostic.MemberOfEnclosingType(fun, _scope.InstanceType!, funSyntax));
                return false;
            }

            if (fun.IsMutatingReceiver && !valueReceiver.IsMutablePlace)
            {
                _diagnostics.ReportError(new Diagnostic.CannotMutateReceiver(valueReceiver, fun, _scope.Owner, funSyntax));
                return false;
            }
        }

        return true;
    }

    
   

    private BoundValue BindCall(CallExprSyntax syntax)
    {
        var callee = BindExpr(syntax.Callee, LookupOptions.MustBeCallable);
        
        if (callee is BoundTypeRef typeRef)
            return BindTypeInit(typeRef, syntax);
        
        var arguments = syntax.ArgumentExprs.Select(BindValue).ToImmutableArray();

        //TODO: Implement named arguments
        if (syntax.ArgList.Arguments.Any(argSyntax => argSyntax.IsNamed))
        {
            return BindError(syntax,
                new Diagnostic.UnsupportedFeature(syntax.ArgList, "Named arguments are not yet implemented."));
        }

        if (callee is BoundError)
            return new BoundError(syntax);
        
        if (callee is not BoundFunGroup calleeFunGroup)
            return BindError(syntax, new Diagnostic.CannotCall(callee));

        var singleFun = BindSingleFunInGroup(calleeFunGroup, arguments, syntax.ArgList);
        if (singleFun is null)
            return new BoundError(syntax);

        var receiver = BindFunReceiver(singleFun, calleeFunGroup.Receiver);
        
        return CheckCallToSingleFun(singleFun, receiver, arguments, calleeFunGroup.Syntax!)
            ? new BoundCall(singleFun, receiver as BoundValue, arguments, syntax)
            : new BoundError(syntax);
    }

    private BoundNode? BindFunReceiver(FunSymbol fun, BoundNode? receiver)
    {
        // Receiver could still be implicit or ambiguous between value or typeref.
        if (receiver is BoundSelf { IsImplicit: true } && fun.IsStatic)
            receiver = null;
        if (receiver is not null)
            receiver = ReplaceBoundValueOrTypeRef(receiver, fun);
        return receiver;
    }
    
    
    /// <summary>
    /// Looks up <paramref name="methodName"/> on <paramref name="receiver"/> and tries
    /// to bind a synthesized call to it. Returns <c>null</c>, if it cannot and does _not_
    /// report any errors.
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

        var funGroup = new BoundFunGroup(funCandidates, receiver);

        // Bind the fun call on a discard binder, so we can report our
        // own error. Note that it cannot attach meaningful information
        // to the sidetable anyway. Since we discard, syntax nodes don't
        // matter expect for the final call bind.
        var resolutionBinder = WithOutput(DiagnosticBag.Discard, new SemanticSideTable());
        var singleFun = resolutionBinder.BindSingleFunInGroup(funGroup, arguments, syntax);
        if (singleFun is null)
            return null;

        return resolutionBinder.CheckCallToSingleFun(singleFun, receiver, arguments, syntax)
            ? new BoundCall(singleFun, receiver, arguments, syntax)
            : null;
    }
    
    
    private BoundValue BindTypeInit(BoundTypeRef typeRef, CallExprSyntax syntax)
    {
        if (typeRef.Type is not StructSymbol @struct || @struct.IsPrimitive)
            return BindError(syntax, new Diagnostic.CannotInitialize(typeRef));

        var uninitializedFields = @struct.Fields.ToList();
        var fieldInits = ImmutableArray.CreateBuilder<BoundFieldInit>();

        var hadError = false;
        foreach (var argSyntax in syntax.ArgList.Arguments)
        {
            if (BindFieldInitializer(@struct, argSyntax) is not BoundFieldInit boundFieldInit)
            {
                hadError = true;
                continue;
            }

            if (!uninitializedFields.Contains(boundFieldInit.Field))
            {
                hadError = true;
                _diagnostics.ReportError(new Diagnostic.FieldAlreadyInitialized(argSyntax, boundFieldInit.Field));
                continue;
            }

            uninitializedFields.Remove(boundFieldInit.Field);
            fieldInits.Add(boundFieldInit);
        }

        if (hadError)
            return new BoundError(syntax);

        if (uninitializedFields.Count > 0)
            return BindError(syntax, new Diagnostic.MissingFieldInitializers([.. uninitializedFields], syntax.ArgList));

        return new BoundStructInit(@struct, fieldInits.ToImmutable(), syntax);
    }

    private BoundValue BindFieldInitializer(StructSymbol @struct, ArgSyntax argSyntax)
    {
        var value = BindValue(argSyntax.Expr);

        var field = @struct.Fields.FirstOrDefault(field => argSyntax.IsNamed
            ? field.Name.SequenceEqual(argSyntax.Name!.Text)
            : field.Name.SequenceEqual(argSyntax.Text));

        if (field is null)
        {
            return argSyntax.IsNamed
                ? BindError(argSyntax, new Diagnostic.UndefinedField(@struct, argSyntax.Name!))
                : BindError(argSyntax, new Diagnostic.MissingFieldName(argSyntax));
        }
        
        if (!CheckAccessibility(field, argSyntax.IsNamed ? argSyntax.Name! : argSyntax) ||
            !CheckType(value, field.Type))
            return new BoundError(argSyntax);

        return new BoundFieldInit(field, value, argSyntax);
    }

    #endregion
}