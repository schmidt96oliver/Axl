using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using Binder = Axl.Compiler.Binding.Binder;

namespace Axl.Compiler;

public class Compilation
{
    public SyntaxTree SyntaxTree { get; }
    public BaseNamespaceSymbol BaseNamespace { get; }

    public BoundFile BoundFile
    {
        get
        {
            field ??= Binder.BindFile(SyntaxTree.FileSyntax, BaseNamespace);
            return field;
        }
    }
    
    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (field.IsDefault)
                field = [.. SyntaxTree.Diagnostics, .. BoundFile.Diagnostics];
            return field;
        }
    }

    
    private Compilation(SyntaxTree syntaxTree)
    {
        SyntaxTree = syntaxTree;
        BaseNamespace = new BaseNamespaceSymbol();
    }

    public static Compilation From(SyntaxTree syntaxTree)
        => new(syntaxTree);


    /// <summary>
    /// The bottom-most <see cref="SyntaxNode"/> that contains the given
    /// <paramref name="location"/>.
    /// </summary>
    public SyntaxNode SyntaxNodeAt(SourceLocation location)
    {
        SyntaxNode currentNode = SyntaxTree.FileSyntax;
        Debug.Assert(currentNode.FullRange.Contains(location.Range));

        while (true)
        {
            var nextNode = currentNode
                .SyntaxNodes()
                .SingleOrDefault(child => child.Range?.Contains(location.Range) == true);

            if (nextNode is null)
                return currentNode;

            currentNode = nextNode;
        }
    }
}