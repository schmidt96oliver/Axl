using AxlCompiler;
using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Text;

namespace Axl.Tests.Compiler;

public sealed partial class SemanticModelTests
{
    public sealed class BoundNodes
    {
        [Theory, Corpus]
        public void Corpus_AllExprSyntaxHaveBoundNodes(string path)
        {
            var tree = Parser.Parse(SourceText.LoadFile(path));
            var compilation = Compilation.From([tree]).WithBaseLibrary();

            var exprs = tree.FileSyntax.Traverse()
                .OfType<ExprSyntax>()
                .Where(expr =>
                    expr.Parent is not AnnotationSyntax && // Annotations and arg names don't produce bound nodes
                    !(expr.Parent is ArgSyntax arg && arg.Name == expr) &&
                    !(expr.Parent is MemberAccessExprSyntax memberAccess &&
                      memberAccess.Member == expr) && // member access left is bound through member access
                    expr.Parent is not FunBodySyntax &&
                    !(expr is IfExprSyntax && expr.Parent is ExprStmtSyntax or IfExprSyntax or ElseClauseSyntax)) // If stmts
                .Where(expr => compilation.SemanticModel.TryBoundNodeFor(expr) is null)
                .ToList();

            if (exprs.Count > 0)
            {
                Assert.Fail($"Some expression syntaxes don't have bound nodes:\n{
                    string.Join("\n", exprs.Select(expr => $"l.{expr.Location.StartLine + 1} \'{expr.Text.ToString()}\'"))
                }");
            }

        }
    }
}