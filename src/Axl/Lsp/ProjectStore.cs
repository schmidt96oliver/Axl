using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public static class ProjectStore
{
    private static readonly Dictionary<DocumentUri, IProject> OpenDocuments = [];
    private static readonly List<IProject> OpenProjects = [];
    

    /// <summary>
    /// Adds the given document to the store.
    /// </summary>
    /// <param name="text">The loaded file text if given. If <c>null</c>, text will be loaded from file system.</param>
    public static void Load(DocumentUri uri, string? text = null)
    {
        //TODO: Handle projects that depend on another
        
        try
        {
            text ??= File.ReadAllText(uri.GetFileSystemPath());
        }
        catch
        {
            // If an error occurred, just remove the entries.
            Unload(uri);
            return;
        }

        var project = OpenProjects.FirstOrDefault(p => p.HasDocument(uri));
        
        if (project is not null)
        {
            // The file references a project, so we need to update it.
            project.UpdateDocument(uri, text);
            OpenDocuments[uri] = project;
            return;
        }
        
        // Load a new project
        var isTaxlFile = Path.GetExtension(uri.GetFileSystemPath()) is ".taxl";
        project = isTaxlFile
            ? TaxlProject.From(uri, text)
            : SingleFileProject.From(uri, text);

        OpenDocuments[uri] = project;
        OpenProjects.Add(project);
    }
    
    public static void Unload(DocumentUri uri)
    {
        if (!OpenDocuments.Remove(uri))
            return;
        
        // The document was open and is now closed.
        // Remove all projects that were only open
        // because of this file.
        OpenProjects.RemoveAll(p => !OpenDocuments.ContainsValue(p));
    }


    /// <summary>
    /// Returns the project, that manages the specified document <paramref name="uri"/>.
    /// If it was not open so far, it tries to load the appropriate project.
    /// Return <c>null</c>, if the project could not be loaded.
    /// </summary>
    public static IProject? TryGetProject(DocumentUri uri)
    {
        if (OpenDocuments.TryGetValue(uri, out var projectFromDoc))
            return projectFromDoc;

        if (OpenProjects.FirstOrDefault(p => p.HasDocument(uri)) is { } openProject)
            return openProject;
        
        Load(uri);
        return OpenDocuments.GetValueOrDefault(uri);
    }

    
    public static Location? TryMapToLsp(SourceLocation axlLocation)
    {
        return OpenProjects
            .Select(p => p.TryMapToLsp(axlLocation))
            .FirstOrDefault(lspLocation => lspLocation is not null);
    }

    public static SourceLocation? TryMapToAxl(Location lspLocation)
    {
        if (OpenDocuments.TryGetValue(lspLocation.Uri, out var project))
            return project.TryMapToAxl(lspLocation);
        return OpenProjects.FirstOrDefault(p => p.HasDocument(lspLocation.Uri))?.TryMapToAxl(lspLocation);
    }
}