using System.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Diagnostics;

/// <summary>
/// What the parser expected at some position, as it should be named in a
/// diagnostic message. Either a concrete <see cref="TokenKind"/> or something
/// coarser, like "an expression".
/// </summary>
public readonly struct ExpectedSyntax
{
    public static readonly ExpectedSyntax Expr = new("an expression");
    public static readonly ExpectedSyntax Stmt = new("a statement");
    public static readonly ExpectedSyntax Body = new("a body");
    public static readonly ExpectedSyntax TypeName = new("a type name");
    public static readonly ExpectedSyntax Member = new("a member ('fn' or 'module')");
    public static readonly ExpectedSyntax ParamList = new("parameters");
    public static readonly ExpectedSyntax String = new("a string");
    public static readonly ExpectedSyntax Param = new("a parameter");
    public static readonly ExpectedSyntax ModuleName = new("a module name");

    private ExpectedSyntax(string description)
        => DisplayName = description;

    private ExpectedSyntax(TokenKind kind)
        => DisplayName = kind.DisplayName;

    public static implicit operator ExpectedSyntax(TokenKind kind) => new(kind);

    /// <summary>
    /// A complete noun phrase, including quoting: <c>"';'"</c>, <c>"an expression"</c>.
    /// </summary>
    public string DisplayName { get; }

    


    public override string ToString() => DisplayName;
}