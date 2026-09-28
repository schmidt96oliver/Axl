using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public enum DirectiveKind
{
    /// <summary>
    /// The test file contains no directive, or it
    /// could not be recognized.
    /// </summary>
    Error,
    
    Check,
    Run,
}

public sealed record Directive(DirectiveKind Kind, SourceLocation Location);