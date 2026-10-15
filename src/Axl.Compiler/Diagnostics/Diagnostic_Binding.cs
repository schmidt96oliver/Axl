using System.Collections.Immutable;
using System.Diagnostics;
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
            => $"Expected type ‘{Expected.Name}‘ but got ‘{Value.Type.Name}‘.";
    }
    
    public sealed record ArgumentTypeMismatch(BoundValue Value, ParameterSymbol Parameter) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Value.Syntax!.Location];

        public override string Message
            => $"Argument for ‘{Parameter.Name}‘ must be ‘{Parameter.Type.Name}‘, but got ‘{Value.Type.Name}‘.";
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
            => [NumberLiteralSyntax.Tree.SourceText.GetLocation(NumberLiteralSyntax.Token.SuffixRange)];

        public override string Message
            => $"Suffix ‘{NumberLiteralSyntax.Token.Suffix.ToString().ToLower()}‘ describes an integral type. Expected a type that describes a decimal number.";
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
                ? $"‘{OperandTypes[0].Name}‘"
                : $"{string.Join(", ", OperandTypes[..^1].Select(expr => $"‘{expr.Name}‘"))} and ‘{OperandTypes[^1].Name}‘";
    }

    public sealed record CannotAssign(BoundNode BoundTarget) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [BoundTarget.Syntax!.Location];

        public override string Message
            => BoundTarget switch
            {
                BoundSelf => "Cannot assign to ‘self‘ directly.",
                BoundValue { IsPlace: false }
                    => "Cannot assign to a temporary value.",
                BoundValue { IsPlace: true, IsMutablePlace: false } value when GetFirstPlace(value) is BoundSelf
                    => "Cannot mutate through ‘self‘, because this fun is not ‘var‘. Consider declaring it ‘var‘.",
                BoundValue { IsPlace: true, IsMutablePlace: false }
                    => "Cannot assign to through a ‘let‘ binding.",
                BoundValue => "Cannot assign this for unknown reason.",

                BoundFunGroup boundFunGroup =>
                    $"Cannot assign to ‘{boundFunGroup.Name}‘, because it is a group of overloaded funs.",
                BoundNamespaceRef boundNamespace =>
                    $"Cannot assign to ‘{boundNamespace.Namespace.Name}‘, because it is a namespace.",
                BoundTypeRef boundType => $"Cannot assign to ‘{boundType.Type.Name}‘, because it is a type.",

                BoundStmt => throw new UnreachableException(
                    "Non-value stmts can never be in assignment target position.")
            };

        private static BoundValue GetFirstPlace(BoundValue value) => value switch
        {
            BoundField field => GetFirstPlace(field.Receiver),
            _ => value
        };
    }

    public sealed record CannotMutateReceiver(BoundValue Receiver, FunSymbol Fun, Symbol Context, SyntaxNode FunSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [FunSyntax.Location];

        public override string Message => Receiver switch
        {
            { IsPlace: false } => $"Cannot call var fun ‘{Fun.Name}‘ on a temporary value.",

            { } when GetFirstPlace(Receiver) is BoundVariable { Variable: { IsReadOnly: true, Name: var varName } }
                => $"Cannot call var fun ‘{Fun.Name}‘ on ‘{varName}‘, because ‘{varName}‘ is declared ‘let‘. Consider declaring it ‘var‘ instead.",

            { } when GetFirstPlace(Receiver) is BoundSelf { IsMutablePlace: false }
                => $"Cannot call var fun ‘{Fun.Name}‘ through ‘self‘, because ‘{ContextFunName}‘ is not ‘var‘. Consider declaring it ‘var‘ instead.",

            _ => $"‘{Fun.Name}‘ is a var fun, but ‘{Receiver!.Type.Name}‘ cannot be mutated here.",
        };

        private string ContextFunName => (Context as FunSymbol)?.Name ?? "???";

        private static BoundValue GetFirstPlace(BoundValue value) => value switch
        {
            BoundField field => GetFirstPlace(field.Receiver),
            _ => value
        };
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
            => $"Both ‘if‘ and ‘else‘ branches must have the same type. Got ‘{First.Type.Name}‘ and ‘{Second.Type.Name}‘.";
    }

    public sealed record UndefinedMember(BoundNode Left, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => Left switch
        {
            BoundValue value => $"‘{value.Type.Name}‘ has no member ‘{Syntax.Text.ToString()}‘.",
            BoundTypeRef type => $"‘{type.Type.Name}‘ has no member ‘{Syntax.Text.ToString()}‘.",
            BoundNamespaceRef ns => $"‘{ns.Namespace.Name}‘ has no member ‘{Syntax.Text.ToString()}‘.",

            _ => $"Cannot find member ‘{Syntax.Text.ToString()}‘.",
        };

    }

    public sealed record CannotConvertToString(BoundValue Node) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Node.Syntax!.Location];

        public override string Message
            => $"Cannot use ‘{Node.Type.Name}‘ in a string, because it has no ‘pub fun ToString()‘ returning ‘String‘.";
        
    }

    public sealed record ArityMismatch(ImmutableArray<FunSymbol> Candidates, int ArgumentCount, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Syntax is ArgListSyntax argList ? argList.InsideLocation : Syntax.Location];

        public override string Message => Candidates.Length == 1
            ? $"‘{Candidates[0].Name}‘ takes {GetArgCountText(Candidates[0].Parameters.Length)}, but {ArgCountWasOrWere(ArgumentCount)} given."
            : $"No overload of ‘{Candidates[0].Name}‘ takes {GetArgCountText(ArgumentCount)}.";

        private static string GetArgCountText(int number)
            => number == 1 ? $"1 argument" : $"{number} arguments";

        private static string ArgCountWasOrWere(int number)
            => number == 1 ? "1 was" : $"{number} were";
    }

    public sealed record CannotCall(BoundNode BoundCallee) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundCallee.Syntax!.Location];

        public override string Message => BoundCallee switch
        {
            BoundValue value => $"A value of type ‘{value.Type.Name}‘ cannot be called.",
            
            IBoundSymbolRef symbolRef => $"{symbolRef.Symbol.ShortDisplayName.Capitalize()} cannot be called.",
            BoundFunGroup fun => $"Fun ‘{fun.Name}‘ cannot be called.",
            BoundStmt => "A statement cannot be called.",
        };
    }

    public sealed record NumberTooBig(TypeSymbol TargetType, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message
            => $"Number is too big for ‘{TargetType.Name}‘.";
    }

    public sealed record DuplicateFunDeclarations(ImmutableArray<FunSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle the name and parameter list, since they are at fault.
            =>
            [
                .. Duplicates.Where(fun => fun.DeclarationSyntax is not null)
                    .Select(fun =>
                        fun.DeclarationSyntax!.Location.SourceText.GetLocation(SourceRange.FromTo(fun.DeclarationSyntax!.Name.Location.Range,
                            fun.DeclarationSyntax!.ParameterList.Location.Range)))
            ];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"‘{Duplicates[0].Name}‘ with the same signature is already declared.";
    }

    public sealed record DuplicateParameters(ImmutableArray<ParameterSymbol> Duplicates) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle all parameter names
            => [.. Duplicates.Where(param => param.DeclarationSyntax is not null).Select(param => param.DeclarationSyntax!.Name.Location)];

        public override string LocationLabel => "Also declared here.";

        public override string Message => $"Duplicate parameter ‘{Duplicates[0].Name}‘.";
    }

    public sealed record NoMatchingOverload(
        ImmutableArray<FunSymbol> Candidates,
        ImmutableArray<TypeSymbol> ArgumentTypes,
        SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations =>
            [Syntax is ArgListSyntax argList ? argList.InsideLocation : Syntax.Location];

        public override string Message => $"""
                                           No overload of ‘{Candidates[0].Name}‘ accepts ({ ArgumentTypes.TypeListString })‘. Candidates:
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
                                           Call to ‘{Candidates[0].Name}‘ is ambiguous between:
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
        public override string Message => $"‘{Fun.Name}‘ must return a value of type ‘{Fun.ReturnType.Name}‘.";
    }

    public sealed record CannotCapture(SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];
        public override string Message => $"Cannot use ‘{Syntax.Text}‘ here. Local funs cannot use variables of the enclosing fun.";
    }

    public sealed record MissingReturn(SyntaxNode BlockSyntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                if (BlockSyntax is BlockExprSyntax blockExprSyntax &&
                    blockExprSyntax.SyntaxElements().First(el => el is Token { Kind: TokenKind.CloseBrace })
                        is Token { IsMissing: false } closeBraceToken)
                {
                    return [closeBraceToken.Location];
                }

                return [BlockSyntax.Location];
            }
        }

        public override string Message
            => "Not all code-paths return a value.";
    }

    public sealed record StaticMemberThroughInstance(FunSymbol Fun, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"{Fun.ShortDisplayName.Capitalize()} is static. Call it as ‘{Fun.OwningType?.Name ?? "???"}.{Fun.Name}(...)‘ instead.";
    }

    public sealed record NotAValue(BoundNode BoundNode) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundNode.Syntax!.Location];

        public override string Message => BoundNode switch
        {
            BoundTypeRef type => $"‘{type.Type.Name}‘ is a type, not a value.",
            BoundNamespaceRef ns => $"‘{ns.Namespace.Name}‘ is a namespace, not a value.",
            BoundFunGroup funGroup => $"‘{funGroup.Name}‘ is a fun, not a value. Did you mean to call it?",

            // TypeRef should of course not report. To be resilient, just include it.
            BoundValue value => $"‘{value.Type.Name}‘ is a value, _not_? a value.",
            BoundStmt => "Expected a value, got a statement.",
        };
    }

    public sealed record NotAType(BoundNode BoundNode) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [BoundNode.Syntax!.Location];

        public override string Message => BoundNode switch
        {
            BoundValue value => $"Expected a type, but got value of type ‘{value.Type.Name}‘.",
            IBoundSymbolRef symbolRef => $"Expected a type, but got {symbolRef.Symbol.ShortDisplayName}.",
            BoundFunGroup funGroup => $"Expected a type, but got fun ‘{funGroup.Name}‘.",
            
            BoundStmt => "Expected a type, but got a statement.",
        };

    }

    public sealed record AlreadyDeclared(ImmutableArray<Symbol> OffendingSymbols) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            // Squiggle the name and parameter list, since they are at fault.
            =>
            [
                .. OffendingSymbols.Where(symbol => GetDeclarationNameSyntax(symbol) is not null)
                    .Select(symbol => GetDeclarationNameSyntax(symbol)!.Location)
            ];

        private static SyntaxElement? GetDeclarationNameSyntax(Symbol symbol) => symbol switch
        {
            StructSymbol { DeclarationSyntax: not null } structSymbol => structSymbol.DeclarationSyntax.Name,
            FunSymbol { DeclarationSyntax: not null } funSymbol => funSymbol.DeclarationSyntax.Name,
            FieldSymbol { DeclarationSyntax: not null } fieldSymbol => fieldSymbol.DeclarationSyntax.Name,
            _ => null
        };
        
        public override string LocationLabel => "Also declared here.";

        public override string Message => $"‘{OffendingSymbols[0].Name}‘ is already declared.";
    }

    public sealed record RecursiveStructLayout(StructSymbol StructSymbol, ImmutableArray<FieldSymbol> FieldPath) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [FieldPath[0].DeclarationSyntax!.TypeExpr.Location];
        public override string Message => $"Struct ‘{StructSymbol.Name}‘ has a cycle in its layout. The cycle is through fields {GetCycleString()}";

        private string GetCycleString()
            => string.Join(" -> ", FieldPath.Select(field => $"‘{field.Owner!.Name}.{field.Name}: {field.Type.Name}‘"));
    }

    public record UndefinedField(StructSymbol Struct, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => $"‘{Struct.Name}‘ has no field ‘{Syntax.Text}‘.";
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

        public override string Message => TypeRef.Type is StructSymbol { IsPrimitive: true } ?
            $"‘{TypeRef.Type.Name}‘ cannot be initialized, because it is a primitive type."
            : $"‘{TypeRef.Type.Name}‘ cannot be initialized.";
    }

    public sealed record NotAccessible(ImmutableArray<Symbol> Symbols, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => Symbols.Length == 1
            ? $"{Symbols[0].ShortDisplayName.Capitalize()} is private and cannot be accessed here."
            : Symbols.All(s => s is FunSymbol)
                ? $"All overloads of {Symbols[0].ShortDisplayName} are private and cannot be accessed here."
                : $"‘{Symbols[0].Name}‘ is private and cannot be accessed here.";
    }

    public sealed record InvalidLocalFunModifier(Token Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => $"‘{Syntax.Text}‘ is not valid on local funs.";
    }

    public sealed record VarLocalFunInStaticContext(Token Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => "‘var‘ is not valid here, because there is no ‘self‘ to mutate.";
    }

    public sealed record VarLocalFunInNonVarFun(Token Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message => "A local fun can only be ‘var‘ if the enclosing fun is ‘var‘.";
    }


    public sealed record StaticFunCannotBeVar(Token Syntax, string FunName) : Error
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

    public sealed record InstanceMemberInStaticContext(Symbol InstanceMember, Symbol Context, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"{InstanceMember.ShortDisplayName.Capitalize()} needs an instance of ‘{InstanceMember.OwningType?.Name ?? "???"}‘, but there is no ‘self‘ here{ GetReasonText() }.";

        private FunSymbol? GetOutermostFun()
            => (FunSymbol?)Context.SelfAndOwners().TakeWhile(s => s is FunSymbol).LastOrDefault();
        
        private string GetReasonText()
            => GetOutermostFun() is { } outermostFun
                ? $", because {outermostFun.ShortDisplayName} is declared ‘static‘"
                : "";
    }

    public sealed record InstanceMemberThroughType(Symbol InstanceMember, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"{InstanceMember.ShortDisplayName.Capitalize()} needs an instance of ‘{InstanceMember.OwningType?.Name ?? "???"}‘. It can not be accessed through the type.";
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
        public override string Message => $"‘{FunGroup.Name}‘ is a fun and has no members.";
    }


    public record MemberOfEnclosingType(Symbol Member, TypeSymbol SelfType, SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        public override string Message =>
            $"Cannot access non-static member {Member.ShortDisplayName} of outer type ‘{Member.OwningType?.Name ?? "???"}‘ via nested type ‘{SelfType.Name}‘.";
    }


    public sealed record OperatorOutsideType(SyntaxNode Syntax) : Error
    {
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [Syntax.Location];

        /// <inheritdoc />
        public override string Message => $"Operator ‘{Syntax.Text}’ can only be declared inside a type.";
    }
}