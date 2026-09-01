#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Declarations;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

string[] inputs = ["""
                    var a = 2;
                    module A { fn NOPE() { } }
                   ""","""
                   module A { }
                   module A.B;
                   fn Fn2_B() { }
                   """,
    
                    """
                    module A;
                    module B;
                    module C { }
                    """,
                    """
                    module A;
                    module B { module C; fn In_B() { } }
                    """
];

var trees = inputs.Select(text => Parser.Parse(SourceFileView.FromText(text))).ToImmutableArray();
var compilation = Compilation.FromTrees(trees);
var declTable = compilation.DeclarationTable;

Console.WriteLine(compilation.Diagnostics);


/*
Console.WriteLine("*** SINGLE");
foreach (var tree in trees)
{
    var roots = declTable.GetRootModuleDecls(tree);
    Console.WriteLine("----");
    foreach (var singleRoot in roots)
        PrintSingleDecl(singleRoot, "");
}

Console.WriteLine("*** MERGED");
PrintMergedDecl(declTable.GlobalDecl, "");

Console.WriteLine("*** Global Symbols");
var globalSymbol = compilation.GlobalModule;
PrintSymbol(globalSymbol, "");


void PrintSingleDecl(ModuleDeclFragment declFragment, string prefix)
{
    var name = declFragment.Name;
    var diagText = declFragment.Diagnostics.Length > 0 ? $"[ERROR x{declFragment.Diagnostics.Length}]" : "";
    var memberText = string.Join(" | ", declFragment.Syntax?.Members.Select(SelectName) ?? []);
    
    Console.WriteLine($"{prefix}{name} {diagText} ({memberText})");
    foreach (var child in declFragment.ChildFragments) PrintSingleDecl(child, prefix + "  ");
}

void PrintMergedDecl(ModuleDecl decl, string prefix)
{
    var name = decl.Name.IsEmpty ? "ROOT" : decl.Name;
    var memberText = string.Join(" | ", decl.Syntaxes.SelectMany(s => s.Members).Select(SelectName));
    
    Console.WriteLine($"{prefix}{name} ({memberText})");
    foreach (var child in decl.ChildModules) PrintMergedDecl(child, prefix + " ");

    
}
string SelectName(MemberSyntax syntax) => syntax switch
{
    BaseModuleDeclSyntax moduleDecl => $"module {string.Join(".", moduleDecl.Name.Parts.Select(t => t.Identifier))}",
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

*/