using System.Collections.Immutable;
using System.Reflection;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Syntax;
using Axl.Compiler.Taxl;
using Binder = Axl.Compiler.Semantics.Binder;

namespace Axl.Compiler;

public class Compilation
{
    public SyntaxTree SyntaxTree { get; }
    public TypeContext TypeContext { get; }

    public HirFile HirFile
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
                field = [.. SyntaxTree.Diagnostics, .. HirFile.Diagnostics];
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
        if (taxlFile.Fragments.Length > 1)
            throw new NotImplementedException("Multiple files not supported yet.");

        return From(Parser.Parse(taxlFile.Fragments[0].SourceView));
    }

    public static Compilation From(SyntaxTree syntaxTree)
        => new(syntaxTree);

}