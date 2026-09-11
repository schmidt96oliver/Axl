using System.Collections.Immutable;
using System.Reflection;
using Axl.Compiler.Binding;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Taxl;
using Binder = Axl.Compiler.Binding.Binder;

namespace Axl.Compiler;

public class Compilation
{
    public SyntaxTree SyntaxTree { get; }
    public TypeContext TypeContext { get; }

    public BoundFile BoundFile
    {
        get
        {
            field ??= Binder.BindFile(SyntaxTree.FileSyntax, TypeContext);
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
        TypeContext = new TypeContext();
    }


    public static Compilation From(TaxlFile taxlFile)
    {
        if (taxlFile.Fragments.OfType<TaxlFragment.Code>().ToList() is not [var codeFragment])
            throw new NotImplementedException("Multiple files not supported yet.");

        return From(Parser.Parse(codeFragment.SourceView));
    }

    public static Compilation From(SyntaxTree syntaxTree)
        => new(syntaxTree);

}