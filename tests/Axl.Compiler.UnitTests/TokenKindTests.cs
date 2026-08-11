using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.UnitTests;

using Shouldly;

public class TokenKindTests
{
    public static TheoryData<TokenKind> AllTokenKinds
        => new(Enum.GetValues<TokenKind>());

    /// <summary>
    /// Kinds whose <see cref="TokenKindDisplayExtensions.DisplayName"/> cannot be lexed back.
    /// Every entry needs a reason. The list should shrink, not grow.
    /// </summary>
    private static bool HasFixedSpelling(TokenKind kind)
        => kind switch
        {
            // Described in prose, because they have no fixed spelling.
            TokenKind.Identifier or TokenKind.NumberLiteral or TokenKind.StringText
                or TokenKind.Comment or TokenKind.Whitespace
                or TokenKind.Error or TokenKind.Eof => false,

            // Spelled '"', but a lone '"' opens a string, so it lexes as StringStart.
            TokenKind.StringEnd => false,

            // 'never' is not a keyword in the lexer yet, so it lexes as an identifier.
            TokenKind.NeverKw => false,

            _ => true,
        };


    [Theory]
    [MemberData(nameof(AllTokenKinds))]
    public void DisplayName_IsDefinedForEveryKind(TokenKind kind)
    {
        // Throws UnreachableException when a kind was added without a DisplayName.
        kind.DisplayName.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(AllTokenKinds))]
    public void DisplayName_LexesBackToItsOwnKind(TokenKind kind)
    {
        if (!HasFixedSpelling(kind))
            Assert.Skip($"'{kind}' has no fixed spelling.");

        var spelling = kind.DisplayName.Replace("'", "");

        var tokens = Lexer.Lex(SourceFileView.FromText(spelling), new DiagnosticBag());

        tokens.Length.ShouldBe(2);
        tokens[0].Kind.ShouldBe(kind);
        tokens[1].Kind.ShouldBe(TokenKind.Eof);
    }
}