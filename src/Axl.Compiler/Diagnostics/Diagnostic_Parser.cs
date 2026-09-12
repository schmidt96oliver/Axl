using System.Collections.Immutable;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnexpectedToken(SourceText Source, Token Actual, ExpectedSyntax Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [SourceLocation.From(Source, Actual.FullRange)];

        public override string Message
            => $"Expected {Expected.DisplayName}, got {Actual.Kind.DisplayName}.";
    }

    public sealed record MissingToken(SourceText Source, Token? Previous, Token Next, ExpectedSyntax Expected) : Error
    {
        public override ImmutableArray<SourceLocation> Locations
        {
            get
            {
                // If it's missing at the start of file, place it
                // before the next token.
                if (Previous is null)
                    return [SourceLocation.From(Source, SourceRange.EmptyBefore(Next.FullRange))];

                return [SourceLocation.From(Source, SourceRange.EmptyAfter(Previous.FullRange))];
            }
        }

        public override string Message
            => $"Expected {Expected.DisplayName}.";
    }

    public sealed record InvalidOperatorChaining(SourceText Source, ImmutableArray<Token> OffendingOperators) : Error
    {
        public override ImmutableArray<SourceLocation> Locations =>
            [.. OffendingOperators.Select(offendingOp => SourceLocation.From(Source, offendingOp.FullRange))];

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