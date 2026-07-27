using Axl.Compiler.Syntax;

namespace Axl.Compiler.Taxl;

public abstract record Directive
{
    public SourceSpan Span { get; }
    
    // Seal it
    private protected Directive(SourceSpan span)
    {
        Span = span;
    }

    public abstract record TestMode(SourceSpan Span) : Directive(Span);
    public abstract record Expect(SourceSpan Span) : Directive(Span);
    
    
    public sealed record Lexer(SourceSpan Span) : TestMode(Span);
    public sealed record Parser(SourceSpan Span) : TestMode(Span);
    public sealed record RunVm(SourceSpan Span, ExpectedRunResult Result) : TestMode(Span);
    public sealed record RunMir(SourceSpan Span, ExpectedRunResult Result) : TestMode(Span);
    public sealed record Compile(SourceSpan Span, ExpectedCompileResult Result) : TestMode(Span);

    public sealed record IgnoreTokens(SourceSpan Span, IReadOnlyList<TokenKind> Kinds) : Directive(Span);
    public sealed record IgnoreDiagnostic(SourceSpan Span, IReadOnlyList<string> Kinds) : Directive(Span);
    
    public sealed record ExpectTokens(SourceSpan Span, IReadOnlyList<TokenKind> Kinds) : Expect(Span);
    public sealed record ExpectOutput(SourceSpan Span, IReadOnlyList<string> Lines) : Expect(Span);
    public sealed record ExpectDiagnostic(SourceSpan Span, int Line, string Id) : Expect(Span);

    public sealed record ScriptBlock(SourceSpan Span, string Text) : Directive(Span);
    public sealed record FileBlock(SourceSpan Span, string Name, string Text) : Directive(Span);
}