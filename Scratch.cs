#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
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
var declTable = new DeclarationTable(compilation.SyntaxTrees);

// Console.WriteLine("*** SINGLE");
// foreach (var tree in trees)
// {
//     var roots = declTable.GetRootModuleDecls(tree);
//     Console.WriteLine("----");
//     foreach (var singleRoot in roots)
//         PrintSingleDecl(singleRoot, "");
// }

Console.WriteLine("*** MERGED");
PrintMergedDecl(declTable.GlobalDecl, "");

Console.WriteLine("*** Global Symbols");
var globalSymbol = new NewModuleSymbol(compilation, declTable.GlobalDecl, Parent: null);
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
    var diagText = decl.Diagnostics.Length > 0 ? $"[ERRORx{decl.Diagnostics.Length}]" : "";
    var memberText = string.Join(" | ", decl.Syntaxes.SelectMany(s => s.Members).Select(SelectName));
    
    Console.WriteLine($"{prefix}{name} {diagText} ({memberText})");
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


foreach (var diagnostic in compilation.GetDiagnostics())
{
    Console.WriteLine($"{diagnostic.DefaultSeverity.ToString().ToUpper()} {diagnostic.Id}: {diagnostic.Message}");
}

var table = compilation.GetSymbolTable();

// foreach (var symbol in table.AllSymbols)
//     Console.WriteLine($"{symbol.Path}: {symbol.GetType().Name} {symbol.Name} Parent = {symbol.Parent?.Path.ToString() ?? "<null>"}");
// return;
foreach (var symbol in table.TopLevelSymbols)
{
    PrintSymbol(symbol, "");
    // Console.WriteLine($"Module {symbol.Name}: Parent = {symbol.Parent?.Name ?? "<null>"}");
    // foreach (var memberSymbol in symbol.GetMembers())
    // {
    //     Console.WriteLine($"   {memberSymbol.GetType().Name} {memberSymbol.Name}");
    //     if (memberSymbol is FnSymbol fnSymbol)
    //     {
    //         Console.WriteLine(
    //             $"     - (Binder: {GetBinderChainText(compilation.GetBinderFactory().GetBinderAt(fnSymbol.Syntax))})");
    //         Console.WriteLine($"     - Locals: {string.Join(", ", fnSymbol.GetParameters().Select(param => $"{param.Name} : {param.Type}"))}");
    //     }
    // }
}

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
    var syntaxText = symbol.GetDeclaringSyntaxes().Length > 0
        ? string.Join(", ", symbol.GetDeclaringSyntaxes().Select(MakeSyntaxText))
        : "<none>";
    
    switch (symbol)
    {
        case NewModuleSymbol moduleSymbol:
            
            Console.WriteLine($"{prefix}Module \"{moduleSymbol.Name}\", Path = \"{moduleSymbol.Path}\", Syntax = {syntaxText}");
            foreach (var member in moduleSymbol.GetMembers())
            {
                PrintSymbol(member, prefix + "  ");
            }

            break;

        case FnSymbol fnSymbol:
            Console.WriteLine($"{prefix}Fn \"{fnSymbol.Name}, Path = \"{fnSymbol.Path}\", Syntax = {syntaxText}");
            // var fnPrefix = prefix + "   ";
            // Console.WriteLine($"{fnPrefix}- Parameters:");
            // foreach (var local in fnSymbol.GetParameters())
            //     PrintSymbol(local, fnPrefix + "   ");
            // Console.WriteLine($"{fnPrefix}- HIR:");
            // PrintHir(fnSymbol.GetHir(), fnPrefix + "   ");
            
            break;
        
        case LocalSymbol localSymbol:
            Console.WriteLine($"{prefix}\"{localSymbol.Name}\" : {localSymbol.Type}, Syntax = {syntaxText}");
            break;
        
        case ErrorSymbol errorSymbol:
            Console.WriteLine($"{prefix}ERROR \"{errorSymbol.Name}\", Syntax = {syntaxText}");
            break;
    }
}

void PrintHir(HirNode hirNode, string prefix)
{
    switch (hirNode)
    {
        case HirBody body:
            foreach (var stmt in body.Stmts)
                PrintHir(stmt, prefix);
            break;

        default:
            Console.WriteLine($"{prefix}{hirNode}");

            break;
    }
}

string GetBinderChainText(_Binder binder)
{
    var parentText = binder.Parent is not null ? GetBinderChainText(binder.Parent) : "";

    var symbolName = binder switch
    {
        CompilationBinder => "<compilation>",
        FileBinder fileBinder => fileBinder.SyntaxTree.Source.File.Path ?? "<internal file>",
        ModuleFragmentBinder moduleBinder => moduleBinder.ModuleSymbol.Name,
        FnBinder fnBinder => fnBinder.FnSymbol.Name,

        _ => "???"
    };
    
    return parentText + $"->{binder.GetType().Name} \"{symbolName}\"";
}
