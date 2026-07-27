namespace Axl.Compiler;

/// <summary>
/// A view into a <see cref="SourceFile"/>.
/// This is all the compiler will ever see, so
/// that only parts of a file can be passed into the pipeline.
/// </summary>
public readonly record struct SourceView(SourceFile File, SourceSpan Span)
{
    public static SourceView Whole(SourceFile file)
        => new(file, SourceSpan.FromLength(0, file.Text.Length));
    
    public static SourceView FromFile(string path)
        => Whole(SourceFile.FromFile(path));
}