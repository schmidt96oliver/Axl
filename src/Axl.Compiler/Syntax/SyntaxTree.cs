using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Syntax;

public enum AxlFileKind
{
    ScriptFile,
    ModuleFile
}

public sealed class SyntaxTree
{
    private AxlFileKind? _lazyAxlFileKind = null;
    
    /// <summary>
    /// The <see cref="SyntaxKind.File"/> node spanning the whole file.
    /// </summary>
    public FileSyntax FileSyntax { get; }
    
    public SourceFileView Source { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public bool HasError { get; }

    
    internal SyntaxTree(FileSyntax fileSyntax, SourceFileView source, ImmutableArray<Diagnostic> diagnostics, bool hasError)
    {
        Guard.MustBe(fileSyntax.Kind is SyntaxKind.File);

        FileSyntax = fileSyntax;
        Source = source;
        Diagnostics = diagnostics;
        HasError = hasError;
    }


    /// <summary>
    /// What kind of <see cref="AxlFileKind"/> this syntax tree is.
    /// Files that cannot be determined are classified as <see cref="AxlFileKind.ModuleFile"/>.
    /// </summary>
    public AxlFileKind GetAxlFileKind()
    {
        if (_lazyAxlFileKind is AxlFileKind actualKind)
            return actualKind;

        foreach (var child in FileSyntax.Children)
        {
            switch (child)
            {
                case FileScopedModuleDeclSyntax:
                case ModuleDeclSyntax:
                    _lazyAxlFileKind = AxlFileKind.ModuleFile;
                    return _lazyAxlFileKind.Value;

                case StmtSyntax:
                case FnDeclSyntax:
                    _lazyAxlFileKind = AxlFileKind.ScriptFile;
                    return _lazyAxlFileKind.Value;
            }
        }

        // Could not be determined. Categorize as module file.
        _lazyAxlFileKind = AxlFileKind.ModuleFile;
        return _lazyAxlFileKind.Value;
    }
}