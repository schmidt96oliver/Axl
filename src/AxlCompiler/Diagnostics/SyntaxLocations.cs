using AxlCompiler.Syntax;
using AxlCompiler.Syntax.Tree;
using AxlCompiler.Text;

namespace AxlCompiler.Diagnostics;

/// <summary>
/// Diagnostics get the entire syntax that was bound and use these helpers
/// to find the part they should report at. Every helper falls back to the
/// entire syntax if it does not have the expected shape (e.g. synthesized
/// calls for operators).
/// </summary>
internal static class SyntaxLocations
{
    /// <summary>
    /// The rightmost name of a reference, including its type arguments.
    /// <c>Base.I32</c> → <c>I32</c>, <c>Base.List[I32]</c> → <c>List[I32]</c>.
    /// </summary>
    public static SourceLocation RightmostName(SyntaxNode syntax) => syntax switch
    {
        MemberAccessExprSyntax memberAccess => memberAccess.Member.Location,
        SubscriptExprSyntax subscript => SourceLocation.FromTo(RightmostName(subscript.Left), subscript.ArgList.Location),
        _ => syntax.Location,
    };

    /// <summary>
    /// The rightmost name of the callee, including its type arguments.
    /// <c>a.Foo[I32](1)</c> → <c>Foo[I32]</c>. Anything that is not a call
    /// is treated like the callee itself (see <see cref="RightmostName"/>).
    /// </summary>
    public static SourceLocation Callee(SyntaxNode syntax) => syntax switch
    {
        CallExprSyntax call => RightmostName(call.Callee),
        _ => RightmostName(syntax),
    };

    /// <summary>
    /// The argument list of a call including its parentheses.
    /// <c>Foo(1, 2)</c> → <c>(1, 2)</c>.
    /// </summary>
    public static SourceLocation ArgList(SyntaxNode syntax) => syntax switch
    {
        CallExprSyntax call => call.ArgList.Location,
        _ => syntax.Location,
    };

    /// <summary>
    /// The arguments of a call without its parentheses, or the parentheses if
    /// there are no arguments. <c>Foo(1, 2)</c> → <c>1, 2</c>.
    /// </summary>
    public static SourceLocation InsideArgList(SyntaxNode syntax) => syntax switch
    {
        CallExprSyntax call => call.ArgList.InsideLocation,
        _ => syntax.Location,
    };

    /// <summary>
    /// The type argument list of a subscript, or of the callee of a call.
    /// <c>List[I32]</c> → <c>[I32]</c>, <c>Foo[I32](1)</c> → <c>[I32]</c>.
    /// </summary>
    public static SourceLocation TypeArgList(SyntaxNode syntax) => syntax switch
    {
        SubscriptExprSyntax subscript => subscript.ArgList.Location,
        CallExprSyntax call => TypeArgList(call.Callee),
        _ => syntax.Location,
    };
}
