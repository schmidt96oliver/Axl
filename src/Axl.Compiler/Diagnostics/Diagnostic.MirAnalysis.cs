using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

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
            ? $"{Fun.ShortDisplayName.Capitalize()} is declared to never return, but can reach the end of its body."
            : "Not all code-paths return a value.";
    }
}