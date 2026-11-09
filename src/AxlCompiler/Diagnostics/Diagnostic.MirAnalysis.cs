using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Text;

namespace AxlCompiler.Diagnostics;

public partial record Diagnostic
{
    public sealed record MissingReturn(FunDefinitionSymbol Fun) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                // Report on '}', then '{' then on return annotation, then on entire declaration
                // Same as NeverFunReachesEnd.

                var syntax = Fun.DeclarationSyntax!;
                
                if (syntax.Body.Expr is BlockExprSyntax blockExprSyntax)
                {
                    if (blockExprSyntax.SyntaxElements().First(el => el is Token { Kind: TokenKind.CloseBrace })
                        is Token { IsMissing: false } closeBraceToken)
                    {
                        return [closeBraceToken.Location];
                    }
                    
                    if (blockExprSyntax.SyntaxElements().First(el => el is Token { Kind: TokenKind.OpenBrace })
                        is Token { IsMissing: false } openBraceToken)
                    {
                        return [openBraceToken.Location];
                    }
                }

                if (syntax.ReturnTypeAnnotation is not null)
                    return [syntax.ReturnTypeAnnotation.Location];
                    
                return [syntax.Location];
            }
        }

        public override string Message => Fun.ReturnType == Fun.Compilation.BaseLibrary.Never
            ? $"{Fun.ShortDisplayText.Capitalize()} is declared to never return, but can reach the end of its body."
            : "Not all code-paths return a value.";
    }

    public sealed record InfiniteInstantiation(FunSymbol Fun) : Error
    {
        // It's a global diagnostic.
        /// <inheritdoc />
        public override ImmutableArray<SourceLocation> Locations => [];

        /// <inheritdoc />
        public override string Message => $"Reached the recursion limit while instantiating ‘{Fun.FullNameWithoutOwnTypeArgs}{TypeArgText}‘.";

        /// <inheritdoc />
        public override string Hint => $"‘{Fun.Name}‘ calls itself with a type argument that grows on every call.";

        private string TypeArgText =>
            $"[{string.Join(", ", Fun.TypeArguments.Select(arg => GetTypeText(arg, 0)))}]";

        private string GetTypeText(TypeSymbol symbol, int depth)
        {
            if (symbol.TypeArguments.Length == 0) return symbol.FullNameWithoutOwnTypeArgs;
            if (depth >= 3) return "...";
            return $"{symbol.FullNameWithoutOwnTypeArgs}[{ string.Join(", ", symbol.TypeArguments.Select(arg => GetTypeText(arg, depth + 1))) }]";
        }
    }
}