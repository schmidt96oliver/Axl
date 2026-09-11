using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public enum DirectiveKind
{
    Check,
    RunPass,
    RunPanic
}

public sealed record Directive(DirectiveKind Kind, SourceLocation Location);