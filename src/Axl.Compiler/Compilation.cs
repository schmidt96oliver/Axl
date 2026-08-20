using System.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler;

public class Compilation
{
    private sealed class FileIdTable<T> : Dictionary<FileId, T>
    {
    }

    
    private readonly FileIdTable<SourceFileView> _sourceFileViews = [];
    private readonly FileIdTable<SyntaxTree> _syntaxTrees = [];


    private Compilation()
    {
    }
    
    public static Compilation FromFile(string path)
    {
        var compilation = new Compilation();
        compilation._sourceFileViews.Add(compilation.NewFileId(),
            SourceFileView.FromFile(path));
        return compilation;
    }


    private FileId NewFileId()
    {
        var id = new FileId(_sourceFileViews.Count);
        Debug.Assert(!_sourceFileViews.ContainsKey(id));
        return id;
    }


    public SourceFileView GetSource(FileId fileId)
        => _sourceFileViews[fileId];
    
    public SyntaxTree GetSyntaxTree(FileId fileId)
    {
        if (!_syntaxTrees.TryGetValue(fileId, out var syntaxTree))
        {
            var tree = Parser.Parse(GetSource(fileId));
            _syntaxTrees.Add(fileId, tree);
            return tree;
        }

        return syntaxTree;
    }
}