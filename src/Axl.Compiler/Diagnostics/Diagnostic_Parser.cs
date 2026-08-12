using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnexpectedToken(SourceFileView Source, Token Actual, ExpectedSyntax Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Source.GetLocation(Actual.Span)];

        public override string Message
            => $"Expected {Expected.DisplayName}, got {Actual.Kind.DisplayName}.";
    }

    public sealed record MissingToken(SourceFileView Source, Token? Previous, Token Next, ExpectedSyntax Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                // If it's missing at the start of file, place it
                // before the next token.
                if (Previous is null)
                    return [Source.GetLocation(SourceSpan.EmptyBefore(Next.Span))];

                return [Source.GetLocation(SourceSpan.EmptyAfter(Previous.Span))];
            }
        }

        public override string Message
            => $"Expected {Expected.DisplayName}.";
    }

    public sealed record InvalidOperatorChaining(SourceFileView Source, ImmutableArray<Token> OffendingOperators) : Error
    {
        public override ImmutableArray<SourceLocation> Locations =>
            [.. OffendingOperators.Select(offendingOp => Source.GetLocation(offendingOp.Span))];

        public override string LocationLabel => "Conflicts with this operator.";

        public override string Message
        {
            get
            {
                var commaOps = string.Join(", ", OffendingOperators[..^1].Select(t => t.Kind.DisplayName));
                return $"Cannot chain {commaOps} and {OffendingOperators[^1].Kind.DisplayName}.";
            }
        }
        

        public override string Hint => "Use parentheses to disambiguate :).";
    }
}