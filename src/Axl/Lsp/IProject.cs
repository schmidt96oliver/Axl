using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Lsp;

public interface IProject
{
    public Compilation Compilation { get; }

    /// <summary>
    /// Maps a <see cref="SourceLocation"/> that refers to <see cref="Compilation"/>
    /// to the LSP location. Returns <c>null</c>, if <paramref name="location"/> does
    /// not refer to this project.
    /// </summary>
    public Location? TryMapToLsp(SourceLocation location);
    
    /// <summary>
    /// Maps an LSP location to the <see cref="SourceLocation"/> inside
    /// this <see cref="Compilation"/>. <c>null</c>, if the given location
    /// doesn't refer to this project or is out of bounds.
    /// </summary>
    public SourceLocation? TryMapToAxl(Location location);

    /// <summary>
    /// Whether this project contains the given document <paramref name="uri"/>
    /// </summary>
    public bool HasDocument(DocumentUri uri);
    
    /// <summary>
    /// Returns all <see cref="SyntaxTree"/>s inside the specified file.
    /// </summary>
    public ImmutableArray<SyntaxTree> GetSyntaxTrees(DocumentUri uri);

    /// <summary>
    /// Returns all diagnostics for the specified file.
    /// </summary>
    public ImmutableArray<Diagnostic> GetDiagnostics(DocumentUri uri);

    /// <summary>
    /// Updates the specified document <paramref name="uri"/> to the specified
    /// text. This will reload <see cref="Compilation"/>.
    /// </summary>
    public void UpdateDocument(DocumentUri uri, string text);
}