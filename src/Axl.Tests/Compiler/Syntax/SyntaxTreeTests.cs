using AxlCompiler.Syntax;
using AxlCompiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

public sealed class SyntaxTreeTests
{
    private static string SignificantLeftOf(string input)
    {
        input = input.RemoveCaret(out var caret);
        var tree = Parser.Parse(SourceText.From(input));
        var token = tree.TrySignificantTokenLeftOf(caret);
        return token is not null ? $"\'{token.Text}'" : "<none>";
    }
    
    private static string LeftOf(string input)
    {
        input = input.RemoveCaret(out var caret);
        var tree = Parser.Parse(SourceText.From(input));
        var token = tree.TryTokenLeftOf(caret);
        return token is not null
            ? token.IsMissing
                ? $"??\'{SyntaxFacts.GetText(token.Kind)}\'"
                : $"\'{token.Text.ToString().Replace("\n", "\\n")}'"
            : "<none>";
    }
    
    private static string At(string input)
    {
        input = input.RemoveCaret(out var caret);
        var tree = Parser.Parse(SourceText.From(input));
        var token = tree.TokenAt(caret);
        return token.IsMissing
            ? $"??\'{SyntaxFacts.GetText(token.Kind)}\'"
            : token.Kind is TokenKind.Eof
                ? "<EOF>"
                : $"\'{token.Text.ToString().Replace("\n", "\\n")}'";
    }

    [Fact]
    public void SignificantTokenLeftOf_Empty()
        => InlineSnapshot.Validate(SignificantLeftOf("|"), "<none>");

    [Fact]
    public void SignificantTokenLeftOf_Missing1()
        => InlineSnapshot.Validate(SignificantLeftOf("p.|"), "'.'");

    [Fact]
    public void SignificantTokenLeftOf_Missing2()
        => InlineSnapshot.Validate(SignificantLeftOf("let a = 1 |\nlet b = 2;"), "'1'");

    [Fact]
    public void SignificantTokenLeftOf_Missing3()
        => InlineSnapshot.Validate(SignificantLeftOf("if |p."), "'if'");

    [Fact]
    public void SignificantTokenLeftOf_Missing4()
        => InlineSnapshot.Validate(SignificantLeftOf("fun F( |) {}"), "'('");

    [Fact]
    public void SignificantTokenLeftOf_Missing5()
        => InlineSnapshot.Validate(SignificantLeftOf("\"{p.|}\""), "'.'");

    [Fact]
    public void SignificantTokenLeftOf_Missing6()
        => InlineSnapshot.Validate(SignificantLeftOf("{ let a = 1; |"), "';'");
    
    [Fact]
    public void SignificantTokenLeftOf_AfterWhitespace1()
        => InlineSnapshot.Validate(SignificantLeftOf("   |"), "<none>");
    
    [Fact]
    public void SignificantTokenLeftOf_AfterWhitespace2()
        => InlineSnapshot.Validate(SignificantLeftOf("   |abc"), "<none>");
    
    [Fact]
    public void SignificantTokenLeftOf_Comment1()
        => InlineSnapshot.Validate(SignificantLeftOf("// abc|"), "<none>");
    
    [Fact]
    public void SignificantTokenLeftOf_Comment2()
        => InlineSnapshot.Validate(SignificantLeftOf("// abc\n|"), "<none>");
    
    [Fact]
    public void SignificantTokenLeftOf_Comment3()
        => InlineSnapshot.Validate(SignificantLeftOf("abc // x |"), "'abc'");
    
    [Fact]
    public void SignificantTokenLeftOf_Newline()
        => InlineSnapshot.Validate(SignificantLeftOf("abc // x \n12|"), "'12'");


    [Fact]
    public void TokenLeftOf_Empty()
        => InlineSnapshot.Validate(LeftOf("|"), "<none>");
    
    [Fact]
    public void TokenLeftOf_Missing1()
        => InlineSnapshot.Validate(LeftOf("p.|"), "'.'");
    
    [Fact]
    public void TokenLeftOf_Missing2()
        => InlineSnapshot.Validate(LeftOf("let a = 1 |\nlet b = 2;"), @"' \n'");
    
    [Fact]
    public void TokenLeftOf_Missing3()
        => InlineSnapshot.Validate(LeftOf("if |p."), "' '");
    
    [Fact]
    public void TokenLeftOf_Missing4()
        => InlineSnapshot.Validate(LeftOf("fun F( |) {}"), "' '");
    
    [Fact]
    public void TokenLeftOf_Missing5()
        => InlineSnapshot.Validate(LeftOf("\"{p.|}\""), "'.'");
    
    [Fact]
    public void TokenLeftOf_Missing6()
        => InlineSnapshot.Validate(LeftOf("{ let a = 1; |"), "' '");
    
    [Fact]
    public void TokenLeftOf_Inside1()
        => InlineSnapshot.Validate(LeftOf("   a|bc def"), "'abc'");
    
    [Fact]
    public void TokenLeftOf_Inside2()
        => InlineSnapshot.Validate(LeftOf("   ab|c def"), "'abc'");
    
    [Fact]
    public void TokenLeftOf_Inside3()
        => InlineSnapshot.Validate(LeftOf("   abc| def"), "'abc'");
    
    [Fact]
    public void TokenLeftOf_Inside4()
        => InlineSnapshot.Validate(LeftOf("   abc |def"), "' '");
    
    [Fact]
    public void TokenLeftOf_Inside5()
        => InlineSnapshot.Validate(LeftOf("   abc d|ef"), "'def'");
    
    [Fact]
    public void TokenLeftOf_Inside6()
        => InlineSnapshot.Validate(LeftOf("   abc de|f "), "'def'");
    
    [Fact]
    public void TokenLeftOf_Eof1()
        => InlineSnapshot.Validate(LeftOf("   abc|  "), "'abc'");
    
    [Fact]
    public void TokenLeftOf_Eof2()
        => InlineSnapshot.Validate(LeftOf("   abc  |"), "'  '");
    
    [Fact]
    public void TokenLeftOf_Eof3()
        => InlineSnapshot.Validate(LeftOf("   abc  12|"), "'12'");


    [Fact]
    public void TokenLeftOf_Comment1()
        => InlineSnapshot.Validate(LeftOf("// abc|"), "'// abc'");

    [Fact]
    public void TokenLeftOf_Comment2()
        => InlineSnapshot.Validate(LeftOf("// abc\n|"), @"'\n'");

    [Fact]
    public void TokenLeftOf_Comment3()
        => InlineSnapshot.Validate(LeftOf("abc // x |"), "'// x '");

    [Fact]
    public void TokenLeftOf_Newline()
        => InlineSnapshot.Validate(LeftOf("abc // x \n12|"), "'12'");


    [Fact]
    public void TokenAt_Empty()
        => InlineSnapshot.Validate(At("|"), "<EOF>");

    [Fact]
    public void TokenAt_Missing1()
        => InlineSnapshot.Validate(At("p.|"), "<EOF>");

    [Fact]
    public void TokenAt_Missing2()
        => InlineSnapshot.Validate(At("let a = 1 |\nlet b = 2;"), @"' \n'");

    [Fact]
    public void TokenAt_Missing3()
        => InlineSnapshot.Validate(At("if |p."), "'p'");

    [Fact]
    public void TokenAt_Missing4()
        => InlineSnapshot.Validate(At("fun F( |) {}"), "')'");

    [Fact]
    public void TokenAt_Missing5()
        => InlineSnapshot.Validate(At("\"{p.|}\""), "'}'");

    [Fact]
    public void TokenAt_Missing6()
        => InlineSnapshot.Validate(At("{ let a = 1; |"), "<EOF>");
    
    [Fact]
    public void TokenAt_Inside1()
        => InlineSnapshot.Validate(At("   a|bc def"), "'abc'");
    
    [Fact]
    public void TokenAt_Inside2()
        => InlineSnapshot.Validate(At("   ab|c def"), "'abc'");

    [Fact]
    public void TokenAt_Inside3()
        => InlineSnapshot.Validate(At("   abc d|ef"), "'def'");
    
    [Fact]
    public void TokenAt_Whitespace1()
        => InlineSnapshot.Validate(At("   abc| def"), "' '");

    [Fact]
    public void TokenAt_Whitespace2()
        => InlineSnapshot.Validate(At("   abc|  "), "'  '");
    
    [Fact]
    public void TokenAt_Before()
        => InlineSnapshot.Validate(At("   abc |def"), "'def'");
    
    [Fact]
    public void TokenAt_Eof()
        => InlineSnapshot.Validate(At("   abc  |"), "<EOF>");

    [Fact]
    public void TokenAt_Eof2()
        => InlineSnapshot.Validate(At("p.|"), "<EOF>");
    
    [Fact]
    public void TokenAt_Comment1()
        => InlineSnapshot.Validate(At("// ab|c "), "'// abc '");

    [Fact]
    public void TokenAt_Comment2()
        => InlineSnapshot.Validate(At("abc // x | "), "'// x  '");

    [Fact]
    public void TokenAt_Newline()
        => InlineSnapshot.Validate(At("abc // x |\n"), @"'\n'");
}