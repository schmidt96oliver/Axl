#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

string[] inputs = ["""
                    module A.B;
                    
                    fn InB(: i32, a: string) { }
                   ""","""
                   module A;
                    fn InA() { }
                    fn InA_2() { }
                   """,
    
                    """
                    module A.B;
                    fn InB_2() { }
                    """,
                    """
                    module Other;
                    
                    fn InOther() { }
                    fn InOther2() { }
                    """
];

var trees = inputs.Select(text => Parser.Parse(SourceFileView.FromText(text))).ToImmutableArray();
var compilation = Compilation.FromTrees(trees);

foreach (var diag in compilation.Diagnostics)
    Console.WriteLine($"[ERROR] {diag.Id}: {diag.Message}");

PrintSymbol(compilation.GlobalSymbol, "");

string SelectName(MemberSyntax syntax) => syntax switch
{
    FnDeclSyntax fnDecl => $"fn {fnDecl.Name.Identifier}",
    NativeFnDeclSyntax nativeFnDecl => $"native fn {nativeFnDecl.Name.Identifier}",
    _ => "??"
};
return;

string MakeSyntaxText(SyntaxNode node)
{
    var treeIndex = trees.IndexOf(node.Tree);
    var startLinePos = node.GetLocation().StartLinePosition;
    var endLinePos = node.GetLocation().EndLinePosition;
    var text = startLinePos.Line != endLinePos.Line
        ? $"[{treeIndex}] l.{startLinePos.Line} - l.{endLinePos.Line}"
        : $"[{treeIndex}] l.{startLinePos.Line}";
    return text;
}


void PrintSymbol(Symbol symbol, string prefix)
{
    var syntaxText = symbol.DeclaringSyntaxes.Length > 0
        ? string.Join(", ", symbol.DeclaringSyntaxes.Select(MakeSyntaxText))
        : "<none>";
    
    switch (symbol)
    {
        case GlobalSymbol globalSymbol:
            Console.WriteLine($"{prefix}<global>, Syntax = {syntaxText}");
            foreach (var member in globalSymbol.Members)
            {
                PrintSymbol(member, prefix + "  ");
            }

            break;
        
        case ModuleSymbol moduleSymbol:
            
            Console.WriteLine($"{prefix}Module \"{moduleSymbol.Name}\", Syntax = {syntaxText}");
            foreach (var member in moduleSymbol.Members)
            {
                PrintSymbol(member, prefix + "  ");
            }

            break;

        case FnSymbol fnSymbol:
            // var scope = GetScope(fnSymbol.DeclaringSyntaxes[0]);
            // var scopeTextBuilder = new StringBuilder();
            // while (scope is not null)
            // {
            //     if (scope is FileScope fileScope)
            //         scopeTextBuilder.Append($"<file [{compilation.SyntaxTrees.IndexOf(
            //             fileScope.FileSyntax.Tree)}]>");
            //     else
            //         scopeTextBuilder.Append(scope);
            //     if (scope.Parent is not null)
            //         scopeTextBuilder.Append(" -> ");
            //     scope = scope.Parent;
            // }
            
            Console.WriteLine($"{prefix}Fn \"{fnSymbol.Name}, Syntax = {syntaxText}");
            
            break;
        
        case LocalSymbol localSymbol:
            Console.WriteLine($"{prefix}\"{localSymbol.Name}\" : {localSymbol.Type}, Syntax = {syntaxText}");
            break;
        
        case ErrorSymbol errorSymbol:
            Console.WriteLine($"{prefix}ERROR \"{errorSymbol.Name}\", Syntax = {syntaxText}");
            break;
    }
}

// Scope GetScope(SyntaxNode syntax)
// {
//     switch (syntax)
//     {
//         case FileSyntax fileSyntax:
//         {
//             var parentScope = compilation.GetGloballyDeclaredSymbol(fileSyntax) is ModuleSymbol module
//                 ? GetModuleScope(module)
//                 : new GlobalScope(compilation.GlobalSymbol);
//             return new FileScope(fileSyntax, parentScope);
//         }
//         
//         case FnDeclSyntax fnDeclSyntax:
//         {
//             var parent = GetScope(fnDeclSyntax.Parent!);
//             return new FnScope((FnSymbol)compilation.GetGloballyDeclaredSymbol(fnDeclSyntax)!, parent);
//         }
//         
//         default:
//             throw new UnreachableException();
//     }
// }
//  
// Scope GetModuleScope(ModuleSymbol symbol)
// {
//     var parent = symbol.Parent is ModuleSymbol moduleSymbol
//         ? GetModuleScope(moduleSymbol)
//         : new GlobalScope(compilation.GlobalSymbol);
//     return new ModuleScope(symbol, parent);
// }
//
//
//
//
//
// public record HirNode;
//
// public record HirExpr(AxlType Type) : HirNode;
//
// public record HirBody(ImmutableArray<Diagnostic> Diagnostics) : HirNode;
//
// public record HirBreak(HirExpr? Expr, AxlType Type) : HirExpr(Type);
//
