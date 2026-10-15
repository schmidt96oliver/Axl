using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public sealed class SingleFileProject : IProject
{
    public DocumentUri Uri { get; }

    /// <inheritdoc />
    public Compilation Compilation { get; private set; }

    public SourceText SourceText => Compilation.SyntaxTrees.Single().SourceText;


    private SingleFileProject(DocumentUri uri, Compilation compilation)
    {
        Uri = uri;
        Compilation = compilation;
    }
    
    public static SingleFileProject From(DocumentUri uri, string text)
    {
        var sourceText = SourceText.From(text, uri.GetFileSystemPath());
        var compilation = Compilation.From(sourceText);
        return new SingleFileProject(uri, compilation);
    }
    
    
    /// <inheritdoc />
    public Location? TryMapToLsp(SourceLocation location)
    {
        if (location.SourceText != SourceText)
            return null;
        
        return new Location
        {
            Range = location.GetLspRange(),
            Uri = Uri
        };
    }

    /// <inheritdoc />
    public SourceLocation? TryMapToAxl(Location location)
    {
        if (location.Uri != Uri)
            return null;

        if (location.Range.TryGetAxlRange(SourceText) is not { } range)
            return null;
        
        return SourceText.GetLocation(range);
    }

    /// <inheritdoc />
    public bool HasDocument(DocumentUri uri)
        => Uri == uri;

    /// <inheritdoc />
    public ImmutableArray<SyntaxTree> GetSyntaxTrees(DocumentUri uri)
        => Uri == uri ? Compilation.SyntaxTrees : [];

    /// <inheritdoc />
    public ImmutableArray<Diagnostic> GetDiagnostics(DocumentUri uri)
        => Uri == uri ? [.. DiagnosticConverter.Convert(Compilation.Diagnostics, uri)] : [];

    /// <inheritdoc />
    public void UpdateDocument(DocumentUri uri, string text)
    {
        if (uri != Uri) return;
        Compilation = Compilation.From(SourceText.From(text, uri.GetFileSystemPath()));
    }
}