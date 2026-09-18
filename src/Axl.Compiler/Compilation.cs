using System.Collections.Immutable;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;
using Binder = Axl.Compiler.Binding.Binder;

namespace Axl.Compiler;

public class Compilation
{
    public SyntaxTree SyntaxTree { get; }
    public BaseModuleSymbol BaseModule { get; }

    public BoundFile BoundFile
    {
        get
        {
            field ??= Binder.BindFile(SyntaxTree.FileSyntax, BaseModule);
            return field;
        }
    }
    
    public Analysis Analysis
    {
        get
        {
            field ??= new Analysis(this);
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
        BaseModule = new BaseModuleSymbol();
    }

    public static Compilation From(SyntaxTree syntaxTree)
        => new(syntaxTree);
}