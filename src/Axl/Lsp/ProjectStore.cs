using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Lsp;

public static class ProjectStore
{
    private static readonly Dictionary<DocumentUri, IProject> OpenDocuments = [];
    private static readonly List<IProject> OpenProjects = [];

    
    
    public static IProject BaseLibraryProject
    {
        get
        {
            field ??= BaseLibProject.TryLoad()!;
            return field;
        }
    }
    
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

        if (BaseLibraryProject.HasDocument(uri))
        {
            BaseLibraryProject.UpdateDocument(uri, text);
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
        if (BaseLibraryProject.HasDocument(uri))
        {
            BaseLibraryProject.UpdateDocument(uri, null);
            return;
        }

        if (!OpenDocuments.Remove(uri, out var project))
            return;

        // The document was open and is now closed.
        // Remove all projects that were only open
        // because of this file.
        var forRemoval = OpenProjects.Where(p => !OpenDocuments.ContainsValue(p)).ToList();
        
        if (!forRemoval.Contains(project))
            project.UpdateDocument(uri, null);
        
        foreach (var remove in forRemoval)
        {
            remove.Close();
            OpenProjects.Remove(remove);
        }
    }


    /// <summary>
    /// Returns the project, that manages the specified document <paramref name="uri"/>.
    /// If it was not open so far, it tries to load the appropriate project.
    /// Return <c>null</c>, if the project could not be loaded.
    /// </summary>
    public static IProject? TryGetProject(DocumentUri uri)
    {
        if (BaseLibraryProject.HasDocument(uri))
            return BaseLibraryProject;
        
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
            .Concat([BaseLibraryProject])
            .Select(p => p.TryMapToLsp(axlLocation))
            .FirstOrDefault(lspLocation => lspLocation is not null);
    }

    public static SourceLocation? TryMapToAxl(Location lspLocation)
    {
        if (BaseLibraryProject.HasDocument(lspLocation.Uri))
            return BaseLibraryProject.TryMapToAxl(lspLocation);
        if (OpenDocuments.TryGetValue(lspLocation.Uri, out var project))
            return project.TryMapToAxl(lspLocation);
        return OpenProjects.FirstOrDefault(p => p.HasDocument(lspLocation.Uri))?.TryMapToAxl(lspLocation);
    }

    public static SourceLocation? TryMapToAxl(DocumentUri uri, Position position)
        => TryMapToAxl(new Location { Uri = uri, Range = new Range(position, position) });
}