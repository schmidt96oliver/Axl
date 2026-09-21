using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public enum DirectiveKind
{
    Check,
    Run,
}

public sealed record Directive(DirectiveKind Kind, SourceLocation Location);