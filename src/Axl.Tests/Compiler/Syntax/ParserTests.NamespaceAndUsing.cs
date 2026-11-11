using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    public sealed class NamespaceAndUsing
    {
        [Fact]
        public void NamespaceAfterUsing()
            => InlineSnapshot.Validate(Tree("using A; namespace A;"), """
                ERROR UnexpectedToken@[9, 18): Expected a statement, got 'namespace'.


                UsingDirective
                · 'using'
                · NameRef 'A'
                · ';'
                Garbage 'namespace'
                ExprStmt
                · NameRef 'A'
                · ';'
                """);
        
        [Fact]
        public void NamespaceAfterStmt()
            => InlineSnapshot.Validate(Tree("var a = 1; namespace A;"), """
                ERROR UnexpectedToken@[11, 20): Expected a statement, got 'namespace'.


                VarDecl
                · 'var'
                · 'a'
                · InitializerClause
                · · '='
                · · NumberLiteral '1'
                · ';'
                Garbage 'namespace'
                ExprStmt
                · NameRef 'A'
                · ';'
                """);
        
        [Fact]
        public void UsingAfterStmt()
            => InlineSnapshot.Validate(Tree("let a = 1; using A;"), """
                ERROR UnexpectedToken@[11, 16): Expected a statement, got 'using'.


                VarDecl
                · 'let'
                · 'a'
                · InitializerClause
                · · '='
                · · NumberLiteral '1'
                · ';'
                Garbage 'using'
                ExprStmt
                · NameRef 'A'
                · ';'
                """);
        
        [Fact]
        public void StmtAfterNamespace()
            => InlineSnapshot.Validate(Tree("namespace A; let a = 1;"), """
                ERROR UnexpectedToken@[13, 16): Expected a member, got 'let'.


                NamespaceDecl
                · 'namespace'
                · NamespaceName 'A'
                · ';'
                Garbage 'let' 'a' '=' '1' ';'
                """);
    }
}