#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

string[] inputs = ["""
                    module A.B;
                    module C;
                    
                    fn InB() { }
                   ""","""
                   var a = 2;
                    module A;
                    
                    fn InA() { }
                   """,
    
                    """
                    module A.B;
                    var a = 2;
                    fn InB_2() { }
                    """,
                    """
                    module Other;
                    
                    fn InOther() { }
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