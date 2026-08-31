using System.Collections.Immutable;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnsupportedFeature(SyntaxElement Element) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
            => [Element.GetLocation()];

        public override string Message
            => $"{GetElementText(Element)} is not (yet) supported.";

        private static string GetElementText(SyntaxElement element)
            => element switch
            {
                Token token => token.Kind.DisplayName,
                SyntaxNode node => node.Kind.ToString(),
                _ => "??"
            };
    }

    public sealed record NotAllowedInFileKind(SyntaxNode Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations =>
        [
            // Put error on the first syntax token/element. Normally this would be the
            // first keyword. If not possible, squiggle the entire syntax.
            Syntax.SyntaxElements().FirstOrDefault()?.GetLocation() ?? Syntax.GetLocation()
        ];
        
        public override string Message => $"{Syntax.Kind} is not allowed in {Syntax.Tree.GetAxlFileKind()}." ;
    }

    public sealed record InvalidFileScopedModuleDecl(FileScopedModuleDeclSyntax Syntax) : Error
    {
        public override ImmutableArray<SourceLocation> Locations 
            => [Syntax.GetLocation()];

        public override string Message => Syntax.Parent is FileSyntax
            ? $"File-scoped module declarations must be the first declaration in the file."
            : $"File-scoped module declarations are only allowed on the file scope.";
    }
}