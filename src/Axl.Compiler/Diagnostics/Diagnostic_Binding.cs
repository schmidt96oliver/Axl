using System.Collections.Immutable;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

public partial record Diagnostic
{
    public sealed record TypeMismatch(BoundValue Value, TypeSymbol Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Value.Syntax!.Location];

        public override string Message
            => $"Expected type ‘{Expected.QualifiedName}‘ but got ‘{Value.Type.QualifiedName}‘.";
    }
    
    public sealed record ArgumentTypeMismatch(BoundArgument Argument, ParameterSymbol Parameter) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Argument.Syntax!.Location];

        public override string Message
            => $"Argument for ‘{Parameter.Name}‘ must be ‘{Parameter.TypeDisplayString}‘, but got ‘{Argument.TypeDisplayString}‘.";
    }

    public sealed record MissingInitializer(VarDeclSyntax VarDeclSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [VarDeclSyntax.SyntaxElements().First().Location];

        public override string Message
            => "Initializer must be specified.";
    }

    public sealed record SuffixInvalidForDecimalNumber(NumberLiteralSyntax NumberLiteralSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [NumberLiteralSyntax.Location];

        public override string Message
            => $"Suffix ‘{NumberLiteralSyntax.Token.Suffix.ToString().ToLowerInvariant()}‘ describes an integral type. Expected a type that describes a decimal number.";
    }

    public sealed record UndefinedName(IdNameSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.Location];
    
        public override string Message
            => $"Cannot find name ‘{Syntax.Text.ToString()}‘ in this scope.";
    }

    public sealed record UndefinedOperator(Token Operator, ImmutableArray<TypeSymbol> OperandTypes) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Operator.Location];

        public override string Message
            => $"Operator ‘{Operator.Text}‘ is not defined for { GetTypeString() }.";

        private string GetTypeString()
            => OperandTypes.Length == 1
                ? $"‘{OperandTypes[0].QualifiedName}‘"
                : $"{string.Join(", ", OperandTypes[..^1].Select(expr => $"{expr.QualifiedName}"))} and {OperandTypes[^1].QualifiedName}";
    }

    public sealed record CannotAssign(BoundNode BoundTarget) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [BoundTarget.Syntax!.Location];

        public override string Message =>
            $"Cannot assign to '{BoundTarget.Syntax?.Text.ToString() ?? ""}', because {GetReasonText(BoundTarget)}.";

        internal static string GetReasonText(BoundNode node) =>
            node switch
            {
                BoundValue value => value.PlaceKind switch
                {
                    PlaceKind.Value => "it is a temporary value",
                    PlaceKind.ReadOnlyPlace => value.PlaceRoot switch
                    {
                        BoundVariable { Variable: ParameterSymbol { IsSelf: true } } =>
                            "'self' is read-only in a non-mut fun",
                        BoundVariable { Variable: ParameterSymbol p } => $"parameter '{p.Name}' is not 'mut'",
                        BoundVariable { Variable: var v } => $"'{v.Name}' is declared with 'let'",
                        _ => "it is read-only",
                    },
                    _ => "it is not mutable",

                },
                BoundFunGroup => "it is a group of overloaded funs",
                BoundNamespaceRef => "it is a namespace",
                BoundTypeRef => "it is a type",
                BoundStmt => "it is a statement",
            };
    }

    public sealed record CannotPassAsMut(BoundNode Node) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Node.Syntax!.Location];

        public override string Message =>
            $"Cannot pass '{Node.Syntax?.Text.ToString() ?? ""}' as 'mut', because { CannotAssign.GetReasonText(Node) }.";
    }
    
    public sealed record CannotReturnAsMut(BoundNode Node) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Node.Syntax!.Location];

        public override string Message =>
            $"Cannot return '{Node.Syntax?.Text.ToString() ?? ""}' as 'mut', because { CannotAssign.GetReasonText(Node) }.";
    }
    
    public sealed record MutReturnEscapesScope(BoundValue Value) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Value.Syntax!.Location];

        public override string Message =>
            $"Cannot return '{Value.Syntax?.Text.ToString() ?? ""}' as 'mut', because it only lives until the end of the function.";
    }

    public sealed record CannotMutateReceiver(BoundValue Receiver, FunSymbol Fun, Symbol? Context, SyntaxNode FunSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [FunSyntax.Location];

        public override string Message =>
            $"Cannot call 'mut' fun '{ Fun.QualifiedName }' on '{Receiver.Syntax?.Text.ToString() ?? "self"}', because { CannotAssign.GetReasonText(Receiver) }.";
    }

    public sealed record BreakOrContinueOutsideLoop(ExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.SyntaxElements().First().Location];

        public override string Message => Syntax is BreakExprSyntax
            ? "Break is only valid inside loops."
            : "Continue is only valid inside loops.";
    }

    public sealed record MissingElse(IfExprSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax.SyntaxElements().First().Location];

        public override string Message
            => "‘if‘ must have an ‘else‘ branch if used as an expression.";
    }

    public sealed record IncompatibleBranches(BoundValue First, BoundValue Second) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [First.Syntax!.Location, Second.Syntax!.Location];

        public override string Message
            => $"Both ‘if‘ and ‘else‘ branches must have the same type. Got ‘{First.Type.QualifiedName}‘ and ‘{Second.Type.QualifiedName}‘.";
    }

    public sealed record UndefinedMember(BoundNode Left, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => Left switch
        {
            BoundValue value => $"‘{value.Type.QualifiedName}‘ has no member ‘{Syntax.Text.ToString()}‘.",
            BoundTypeRef type => $"‘{type.Type.QualifiedName}‘ has no member ‘{Syntax.Text.ToString()}‘.",
            BoundNamespaceRef ns => $"‘{ns.Namespace.QualifiedName}‘ has no member ‘{Syntax.Text.ToString()}‘.",

            _ => $"Cannot find member ‘{Syntax.Text.ToString()}‘.",
        };

    }

    public sealed record CannotConvertToString(BoundValue Node) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Node.Syntax!.Location];

        public override string Message
            => $"Cannot use ‘{Node.Type.QualifiedName}‘ in a string, because it has no ‘pub fun ToString()‘ returning ‘String‘.";
        
    }

    public sealed record ArityMismatch(ImmutableArray<FunSymbol> Candidates, int ArgumentCount, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax is ArgListSyntax argList ? argList.InsideLocation : Syntax.Location];

        public override string Message => Candidates.Length == 1
            ? $"‘{Candidates[0].QualifiedName}‘ takes {GetArgCountText(Candidates[0].Parameters.Length)}, but {ArgCountWasOrWere(ArgumentCount)} given."
            : $"No overload of ‘{Candidates[0].QualifiedName}‘ takes {GetArgCountText(ArgumentCount)}.";

        private static string GetArgCountText(int number)
            => number == 1 ? $"1 argument" : $"{number} arguments";

        private static string ArgCountWasOrWere(int number)
            => number == 1 ? "1 was" : $"{number} were";
    }
    
    public sealed record TypeParamArityMismatch(ImmutableArray<FunSymbol> Candidates, BoundFunGroup FunGroup) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [FunGroup.TypeArgListSyntax?.Location ?? FunGroup.Syntax!.Location];

        public override string Message => Candidates.Length == 1
            ? $"‘{Candidates[0].QualifiedName}‘ takes {GetArgCountText(Candidates[0].TypeParameters.Length)}, but {ArgCountWasOrWere(FunGroup.TypeArguments.Length)} given."
            : $"No overload of ‘{Candidates[0].QualifiedName}‘ takes {GetArgCountText(FunGroup.TypeArguments.Length)}.";

        private static string GetArgCountText(int number)
            => number == 1 ? $"1 type argument" : $"{number} type arguments";

        private static string ArgCountWasOrWere(int number)
            => number == 1 ? "1 was" : $"{number} were";
    }

    public sealed record CannotCall(BoundNode BoundCallee) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundCallee.Syntax!.Location];

        public override string Message => BoundCallee switch
        {
            BoundValue value => $"A value of type ‘{value.Type.QualifiedName}‘ cannot be called.",
            
            IBoundSymbolRef symbolRef => $"{symbolRef.Symbol.ShortDisplayName.Capitalize()} cannot be called.",
            BoundFunGroup fun => $"Fun ‘{fun.Candidates[0].QualifiedName}‘ cannot be called.",
            BoundStmt => "A statement cannot be called.",
        };
    }

    public sealed record NumberTooBig(TypeSymbol TargetType, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message
            => $"Number is too big for ‘{TargetType.QualifiedName}‘.";
    }

    public sealed record DuplicateFunDeclarations(ImmutableArray<FunSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle the name and parameter list, since they are at fault.
            =>
            [
                .. Duplicates.Select(fun =>
                        SourceLocation.FromTo(fun.DeclarationSyntax!.Name.Location,
                            fun.DeclarationSyntax!.ParameterList.Location))
            ];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"‘{Duplicates[0].QualifiedName}‘ with the same signature is already declared.";
    }

    public sealed record DuplicateParameters(ImmutableArray<ParameterSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle all parameter names
            => [.. Duplicates.Where(param => param.DeclarationSyntax is not null).Select(param => param.DeclarationSyntax!.Name.Location)];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"Duplicate parameter ‘{Duplicates[0].Name}‘.";
    }
    
    public sealed record DuplicateTypeParameters(ImmutableArray<TypeParameterSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle all parameter names
            => [.. Duplicates.Where(param => param.DeclarationSyntax is not null).Select(param => param.DeclarationSyntax!.Name.Location)];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"Duplicate type parameter ‘{Duplicates[0].Name}‘.";
    }

    public sealed record NoMatchingOverload(
        ImmutableArray<FunSymbol> Candidates,
        ImmutableArray<BoundArgument> Arguments,
        SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations =>
            [Syntax is ArgListSyntax argList ? argList.InsideLocation : Syntax.Location];

        public override string Message => $"""
                                           No overload of ‘{Candidates[0].QualifiedName}‘ accepts ({ Arguments.TypeListString })‘. Candidates:
                                           {CandidateString}
                                           """;

        private string CandidateString =>
            string.Join('\n', Candidates
                .Take(5)
                .Select(fun => fun.DisplayNameWithParameterTypes))
            + (Candidates.Length > 5 ? $"... and { Candidates.Length - 5 } more." : "");
    }

    public sealed record AmbiguousCall(
        ImmutableArray<FunSymbol> Candidates,
        SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations =>
            [Syntax is ArgListSyntax argList ? argList.InsideLocation : Syntax.Location];

        public override string Message => $"""
                                           Call to ‘{Candidates[0].QualifiedName}‘ is ambiguous between:
                                           {CandidateString}
                                           """;

        private string CandidateString =>
            string.Join('\n', Candidates
                .Take(5)
                .Select(fun => fun.DisplayNameWithParameterTypes))
            + (Candidates.Length > 5 ? $"... and {Candidates.Length - 5} more." : "");
    }

    public sealed record CannotShadow(Symbol ShadowedSymbol, IdentifierToken NameSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [NameSyntax.Location];

        public override string Message
            => $"‘{ShadowedSymbol.Name}‘ is already declared on this scope. Only variables can be shadowed.";
    }

    public sealed record MissingReturnValue(FunSymbol Fun, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"‘{Fun.Name}‘ must return a value of type ‘{Fun.ReturnType.QualifiedName}‘.";
    }

    public sealed record CannotCapture(SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Cannot use ‘{Syntax.Text}‘ here. Local funs cannot use variables of the enclosing fun.";
    }

    public sealed record StaticMemberThroughInstance(FunSymbol Fun, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"{Fun.ShortDisplayName.Capitalize()} is static. Call it as ‘{Fun.QualifiedName}(...)‘ instead.";
    }

    public sealed record NotAValue(BoundNode BoundNode) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundNode.Syntax!.Location];

        public override string Message => BoundNode switch
        {
            BoundTypeRef type => $"‘{type.Type.QualifiedName}‘ is a type, not a value.",
            BoundNamespaceRef ns => $"‘{ns.Namespace.QualifiedName}‘ is a namespace, not a value.",
            BoundFunGroup funGroup => $"‘{funGroup.Candidates[0].QualifiedName}‘ is a fun, not a value. Did you mean to call it?",

            // TypeRef should of course not report. To be resilient, just include it.
            BoundValue value => $"‘{value.Type.QualifiedName}‘ is a value, _not_? a value.",
            BoundStmt => "Expected a value, got a statement.",
        };
    }

    public sealed record NotAType(BoundNode BoundNode) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundNode.Syntax!.Location];

        public override string Message => BoundNode switch
        {
            BoundValue value => $"Expected a type, but got value of type ‘{value.Type.QualifiedName}‘.",
            IBoundSymbolRef symbolRef => $"Expected a type, but got {symbolRef.Symbol.ShortDisplayName}.",
            BoundFunGroup funGroup => $"Expected a type, but got fun ‘{funGroup.Candidates[0].QualifiedName}‘.",
            
            BoundStmt => "Expected a type, but got a statement.",
        };

    }
    public sealed record NotANamespace(BoundNode BoundNode) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundNode.Syntax!.Location];

        public override string Message => BoundNode switch
        {
            BoundValue value => $"Expected a namespace, but got value of type ‘{value.Type.QualifiedName}‘.",
            IBoundSymbolRef symbolRef => $"Expected a namespace, but got {symbolRef.Symbol.ShortDisplayName}.",
            BoundFunGroup funGroup => $"Expected a namespace, but got fun ‘{funGroup.Candidates[0].QualifiedName}‘.",
            
            BoundStmt => "Expected a namespace, but got a statement.",
        };

    }

    public sealed record AlreadyDeclared(ImmutableArray<Symbol> OffendingSymbols) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle the name and parameter list, since they are at fault.
            =>
            [
                .. OffendingSymbols.SelectMany(symbol => GetDeclNameLocations(symbol))
            ];

        private static ImmutableArray<SourceLocation> GetDeclNameLocations(Symbol symbol) => symbol switch
        {
            SourceStructSymbol structSymbol => [structSymbol.DeclarationSyntax.Name.Location],
            SourceFunSymbol sourceFun => [sourceFun.DeclarationSyntax.Name.Location],
            FieldSymbol fieldSymbol => [fieldSymbol.DeclarationSyntax.Name.Location],
            SourceNamespaceSymbol namespaceSymbol =>
            [
                .. namespaceSymbol.Declaration.NamespaceDeclSyntaxes.Select(s => s.Name.Location)
            ],
            _ => []
        };
        
        public override string LocationLabel => "Also declared here.";

        public override string Message => $"‘{OffendingSymbols[0].QualifiedName}‘ is already declared.";
    }

    public sealed record RecursiveStructLayout(StructSymbol StructSymbol, ImmutableArray<FieldSymbol> FieldPath) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [FieldPath[0].DeclarationSyntax.TypeExpr.Location];
        public override string Message => $"Struct ‘{StructSymbol.QualifiedName}‘ has a cycle in its layout. The cycle is through fields {GetCycleString()}";

        private string GetCycleString()
            => string.Join(" -> ", FieldPath.Select(field => $"‘{field.QualifiedName}: {field.Type.QualifiedName}‘"));
    }

    public record UndefinedField(StructSymbol Struct, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => $"‘{Struct.QualifiedName}‘ has no field ‘{Syntax.Text}‘.";
    }

    public record MissingFieldName(SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => "Specify which field this initializes. Like ‘x = value‘.";
    }

    public sealed record MissingFieldInitializers(ImmutableArray<FieldSymbol> MissingFields, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Not all fields are initialized. Missing: {GetFieldNameString()}.";

        private string GetFieldNameString()
            => MissingFields.Length == 1
                ? $"‘{MissingFields[0].Name}‘"
                : $"{string.Join(", ", MissingFields[..^1].Select(f => $"‘{f.Name}‘"))} and ‘{MissingFields[^1].Name}‘";
    }

    public sealed record FieldAlreadyInitialized(ArgSyntax ArgSyntax, FieldSymbol Field) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [ArgSyntax.IsNamed ? ArgSyntax.Name!.Location : ArgSyntax.Location];
        public override string Message => $"Field ‘{Field.Name}‘ is already initialized.";
    }

    public sealed record CannotInitialize(BoundTypeRef TypeRef) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [TypeRef.Syntax!.Location];

        public override string Message => TypeRef.Type switch
        {
            StructSymbol { IsPrimitive: true } =>
                $"‘{TypeRef.Type.QualifiedName}‘ cannot be initialized, because it is a primitive type.",
            TypeParameterSymbol =>
                $"‘{TypeRef.Type.QualifiedName}‘ cannot be initialized, because it is a generic type.",
            _ => $"‘{TypeRef.Type.QualifiedName}‘ cannot be initialized."
        };
    }

    public sealed record NotAccessible(ImmutableArray<Symbol> Symbols, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => Symbols.Length == 1
            ? $"{Symbols[0].ShortDisplayName.Capitalize()} is private and cannot be accessed here."
            : Symbols.All(s => s is FunSymbol)
                ? $"All overloads of {Symbols[0].ShortDisplayName} are private and cannot be accessed here."
                : $"‘{Symbols[0].ShortDisplayName}‘ is private and cannot be accessed here.";
    }


    public sealed record StaticFunCannotBeMut(Token Syntax, string FunName) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"‘{FunName}‘ cannot be ‘{Syntax.Text}‘, because it is static.";
    }

    public sealed record InvalidSelfRef(SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => "‘self‘ is only available inside non-static funs of a type.";
    }

    public sealed record InstanceMemberInStaticContext(Symbol InstanceMember, Symbol? Context, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"{InstanceMember.ShortDisplayName.Capitalize()} needs an instance of ‘{InstanceMember.OwningType?.QualifiedName ?? "???"}‘, but there is no ‘self‘ here{ GetReasonText() }.";

        private FunSymbol? GetOutermostFun()
            => (FunSymbol?)Context?.SelfAndOwners().TakeWhile(s => s is FunSymbol).LastOrDefault();
        
        private string GetReasonText()
            => GetOutermostFun() is { } outermostFun
                ? $", because {outermostFun.ShortDisplayName} is declared ‘static‘"
                : "";
    }

    public sealed record InstanceMemberThroughType(Symbol InstanceMember, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"{InstanceMember.ShortDisplayName.Capitalize()} needs an instance of ‘{InstanceMember.OwningType?.QualifiedName ?? "???"}‘. It can not be accessed through the type.";
    }

    public sealed record AmbiguousReference(LookupResult LookupResult, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"‘{Syntax.Text}‘ is ambiguous. It could refer to { GetSymbolsText() }.";

        private string GetSymbolsText()
        {
            return LookupResult.Symbols.Length switch
            {
                0 => "no symbol",
                1 => LookupResult.Symbols[0].ShortDisplayName,
                _ => string.Join(", ", LookupResult.Symbols[..^1].Select(s => s.ShortDisplayName))
                     + " or "
                     + LookupResult.Symbols[^1].ShortDisplayName,
            };
        }
    }

    public sealed record FunHasNoMembers(BoundFunGroup FunGroup, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"‘{FunGroup.Candidates[0].QualifiedName}‘ is a fun and has no members.";
    }


    public record MemberOfEnclosingType(Symbol Member, TypeSymbol SelfType, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"Cannot access non-static member {Member.Name} of outer type ‘{Member.OwningType?.QualifiedName ?? "???"}‘ via nested type ‘{SelfType.Name}‘.";
    }


    public sealed record OperatorOutsideType(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"Operator ‘{Syntax.Text}’ can only be declared outside a type.";
    }

    public sealed record OperatorCannotBeStatic(string FunName, Token Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message =>
            $"Operator ‘{FunName}’ cannot be ‘static’. It is called on a value and uses ‘self’ as its left operand.";
    }

    public sealed record OperatorCannotBeMut(string FunName, Token Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message =>
            $"Operator ‘{FunName}’ cannot be ‘mut’.";
    }

    public sealed record WrongOperatorParameterCount(FunSymbol Fun) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Fun.DeclarationSyntax!.ParameterList.Location];

        /// <inheritdoc />
        public override string Message
        {
            get
            {
                var isUnary = SyntaxFacts.IsUnaryOperator(Fun.DeclarationSyntax!.Name.Token.Kind);
                var isBinary = SyntaxFacts.IsBinaryOperator(Fun.DeclarationSyntax.Name.Token.Kind);
                return (isUnary, isBinary) switch
                {
                    (true, true) =>
                        $"Operator ‘{Fun.Name}’ must take 0 parameters (unary) or 1 parameter (binary), but takes {Fun.Parameters.Length}.",
                    (true, false) =>
                        $"Unary operator ‘{Fun.Name}’ must take 0 parameters, but takes {Fun.Parameters.Length}.",
                    (false, true) =>
                        $"Binary operator ‘{Fun.Name}’ must take 1 parameter, but takes {Fun.Parameters.Length}.",
                    _ => $"Wrong parameter count for operator ‘{Fun.Name}’."
                };
            }
        }
    }

    public sealed record WrongOperatorReturnType(FunSymbol Fun, TypeSymbol Expected) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations =>
        [
            Fun.DeclarationSyntax!.ReturnTypeAnnotation?.Location ?? Fun.DeclarationSyntax.Name.Location
        ];

        /// <inheritdoc />
        public override string Message => 
            $"Operator ‘{Fun.Name}’ must return ´{Expected.Name}´, but returns ’{Fun.ReturnType.Name}’.";
    }

    public sealed record DuplicateNamespaceDeclaration(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => "A file can only have one namespace declaration.";
    }

    public sealed record StmtInNamespaceFile(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => "Statements are not allowed in a namespace file.";

        /// <inheritdoc />
        public override string Hint => "Remove the namespace declaration at the top of this file to make it a script.";
    }

    public sealed record NamespaceAlreadyDeclared(NamespaceDeclaration Declaration) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations =>
        [
            .. Declaration.NamespaceDeclSyntaxes.Select(s => s.Name.Location)
        ];

        /// <inheritdoc />
        public override string Message =>
            $"The namespace '{Declaration.Name}' is already declared in a dependency. Namespaces are only open across the same project.";
    }

    public sealed record NamespaceDeclarationInScript(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => "The namespace declaration must be the first declaration in the file.";
    }

    public sealed record InvalidScriptStructModifier(Token Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"‘{Syntax.Text}‘ is not valid on structs inside scripts.";
    }

    public sealed record InconsistentAccessibility(BoundTypeRef TypeRef, Symbol? Owner) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [TypeRef.Syntax!.Location];

        /// <inheritdoc />
        public override string Message => Owner is not null
            ? $"{TypeRef.Type.ShortDisplayName} is less accessible than {Owner.ShortDisplayName}."
            : $"{TypeRef.Type.ShortDisplayName} is not globally accessible.";
    }

    public sealed record UsingDirectiveAfterCode(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => "Using directives must appear before all declarations and code.";
    }

    public sealed record UnnecessaryUsing(UsingDirectiveSyntax Syntax) : Lint
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Info;

        /// <inheritdoc />
        public override string Message => "The using directive is not necessary and can safely be removed.";
    }

    public sealed record UnknownAnnotation(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"Unknown annotation '{Syntax.Text}'.";
    }

    public sealed record InvalidStructAnnotation(BoundAnnotation Annotation) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Annotation.Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"Annotation '{Annotation.Syntax.Text}' is not valid for structs.";
    }

    public sealed record InvalidFunAnnotation(BoundAnnotation Annotation) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Annotation.Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"Annotation '{Annotation.Syntax.Text}' is not valid for funs.";
    }

    public sealed record UnknownPrimitive(BoundAnnotation Annotation, string Name) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Annotation.Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"Unknown primitive '{Name}'.";
    }

    public sealed record FieldDeclarationOnPrimitive(FieldDeclSyntax Syntax, StructSymbol Symbol) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"Cannot declare fields on {Symbol.ShortDisplayName}, because it is primitive.";
    }


    public sealed record UnknownIntrinsic(BoundAnnotation Annotation) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Annotation.Syntax.Location];

        /// <inheritdoc />
        public override string Message => "There is no intrinsic that matches the given signature.";
    }

    public sealed record IntrinsicFunWithBody(FunSymbol Fun, FunBodySyntax Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"{Fun.ShortDisplayName} cannot declare a body, because it is intrinsic.";
    }

    public record MissingFunBody(FunSymbol Fun, FunDeclSyntax Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations =>
            [SourceLocation.FromTo(Syntax.Name.Location, Syntax.ParameterList.Location)];

        /// <inheritdoc />
        public override string Message => $"{Fun.ShortDisplayName} must declare a body.";
    }

    public sealed record CannotConcatString(SyntaxNode Syntax, TypeSymbol String) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message =>
            $"Cannot build interpolated string, because '{String.QualifiedName}.{Names.StringConcatMethod}' does not exist.";
    }

    public sealed record MutNotAllowedHere(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => "'mut' is only allowed on arguments or after return.";
    }

    public sealed record UnexpectedMutReturn(MutExprSyntax Syntax, FunSymbol Fun) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.MutKw.Location];

        /// <inheritdoc />
        public override string Message =>
            $"‘{Fun.Name}’ does not return ‘mut’. Remove ‘mut’ or change the return type to ‘mut {Fun.ReturnType.Name}’.";
    }
    
    public sealed record MissingMutReturn(SyntaxNode Syntax, FunSymbol Fun) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message =>
            $"‘{Fun.Name}’ returns ‘mut {Fun.ReturnType.QualifiedName}’. Use ‘return mut ...’.";
    }

    public sealed record SubscriptNotAllowed(SubscriptArgListSyntax Syntax, BoundNode Left) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message 
            => "Subscript is not allowed here. It can only be used to name type arguments.";
    }
}