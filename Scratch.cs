#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Semantics.Binders;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax.Tree;


var input = """
            module A
            {
                module B
                {
                    module C { fn Test1() { } fn Test2() { } }
                    fn Test2() { }
                }
            }
            module A.B.C { fn TestInC() { } }
            module A
            {
                fn TestInA() { }
                module B.C { fn TestInC2() { } }
            }
            """;

var compilation = Compilation.FromText(input);
var table = compilation.GetSymbolTable();
// foreach (var symbol in table.AllSymbols)
//     Console.WriteLine($"{symbol.Path}: {symbol.GetType().Name} {symbol.Name} Parent = {symbol.Parent?.Path.ToString() ?? "<null>"}");
// return;
foreach (var symbol in table.AllSymbols.Where(symbol => symbol.Parent is null))
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

void PrintSymbol(Symbol symbol, string prefix)
{
    switch (symbol)
    {
        case ModuleSymbol moduleSymbol:
            Console.WriteLine($"{prefix}Module \"{moduleSymbol.Name}\", Path = \"{moduleSymbol.Path}\"");
            foreach (var member in moduleSymbol.GetMembers())
            {
                PrintSymbol(member, prefix + "  ");
            }

            break;

        case FnSymbol fnSymbol:
            Console.WriteLine($"{prefix}Fn \"{fnSymbol.Name}, Path = \"{fnSymbol.Path}\"");
            // var fnPrefix = prefix + "   ";
            // Console.WriteLine($"{fnPrefix}- Parameters:");
            // foreach (var local in fnSymbol.GetParameters())
            //     PrintSymbol(local, fnPrefix + "   ");
            // Console.WriteLine($"{fnPrefix}- HIR:");
            // PrintHir(fnSymbol.GetHir(), fnPrefix + "   ");
            
            break;
        
        case LocalSymbol localSymbol:
            Console.WriteLine($"{prefix}\"{localSymbol.Name}\" : {localSymbol.Type}");
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

string GetBinderChainText(Binder binder)
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

// Get member binders per syntax tree



// public abstract class Symbol(SymbolName name)
// {
//     public SymbolName Name { get; } = name;
// }
//
// public sealed class CompilationExt
// {
//     private readonly ImmutableArray<SyntaxTree> _syntaxTrees;
//
//
//     public CompilationExt(ImmutableArray<SyntaxTree> syntaxTrees)
//     {
//         _syntaxTrees = syntaxTrees;
//     }
//
//     private ImmutableArray<ModuleSymbol.Fragment> GetModuleFragments()
//     {
//         var compilationBinder = new CompilationBinder(typeContext);
//         
//         var fragments = ImmutableArray.CreateBuilder<ModuleSymbol.Fragment>();
//
//         foreach (var tree in _syntaxTrees)
//         {
//             var fileBinder = new FileBinder(tree, parent: compilationBinder);
//
//             foreach (var moduleDeclSyntax in tree.FileSyntax.Members.OfType<ModuleDeclSyntax>())
//             {
//                 
//             }
//         }
//     }
//     
//     public ImmutableArray<ModuleSymbol> GetModules()
//     {
//         throw new NotImplementedException();
//     }
//     
//     public HirBody GetScriptHir()
//     {
//         throw new NotImplementedException();
//     }
// }
//
// // public sealed class ModuleFragment
// // {
// //     public Binder FragmentBinder { get; }
// //     
// //     public ModuleDeclSyntax Syntax { get; }
// //
// //     public ModuleFragmentSymbol(SymbolName moduleName, ModuleDeclSyntax syntax, Binder fragmentBinder)
// //         :base(moduleName)
// //     {
// //         Syntax = syntax;
// //         FragmentBinder = fragmentBinder;
// //     }
// //     
// //     public ImmutableArray<Symbol> GetMembers()
// //     {
// //         var builder = ImmutableArray.CreateBuilder<Symbol>();
// //         foreach (var memberSyntax in Syntax.Members)
// //         {
// //             builder.Add(FragmentBinder.BindMember(memberSyntax));
// //         }
// //
// //         return builder.DrainToImmutable();
// //     }
// // }
//
// public sealed class ModuleSymbol : Symbol
// {
//     public readonly record struct Fragment(string Path, ModuleDeclSyntax Syntax, Binder Binder);
//     
//     private ImmutableArray<Fragment> Fragments { get; }
//
//
//     public ModuleSymbol(SymbolName name, ImmutableArray<ModuleSymbol.Fragment> fragments)
//         : base(name)
//     {
//         Fragments = fragments;
//     }
//
//
//     public ImmutableArray<Symbol> GetMembers()
//     {
//         var builder = ImmutableArray.CreateBuilder<Symbol>();
//         foreach (var fragment in Fragments)
//         foreach (var memberSyntax in fragment.Syntax.Members)
//         {
//             builder.Add(fragment.Binder.BindMember(memberSyntax));
//         }
//
//         return builder.DrainToImmutable();
//     }
// }
//
// public sealed class FnSymbol : Symbol
// {
//     public FnDeclSyntax Syntax { get; }
//     public FnBinder Binder { get; }
//
//     public FnSymbol(SymbolName name, FnDeclSyntax syntax, FnBinder binder) :
//         base(name)
//     {
//         Syntax = syntax;
//         Binder = binder;
//     }
//     
//     public ImmutableArray<AxlType> GetParameterTypes()
//     {
//         //TODO: Lazy eval
//         return [.. Syntax.Parameters.Select(paramSyntax => Binder.BindType(paramSyntax.TypeAnnotation!))];
//     }
//
//     public AxlType GetReturnType()
//     {
//         //TODO: Lazy eval
//         return Binder.BindType(Syntax.ReturnTypeAnnotation ?? throw new Exception("ReturnTypeAnnotation required."));
//     }
//     
//     public HirBody GetHir()
//     {
//         //TODO: Lazy eval
//         return Binder.BindBody(Syntax.Body);
//     }
// }
//
// public sealed class LocalSymbol(SymbolName name) : Symbol(name)
// {
//     public AxlType Type { get; }
//     
//     public HirExpr Initializer { get; }
// }
//



// public class GlobalBinderContextBuilder
// {
//     public static BinderContext Build(Compilation compilation, SyntaxTree tree)
//     {
//         var globalScope = new TempGlobalScope();
//
//         foreach (var memberSyntax in tree.FileSyntax.Members)
//         {
//             var memberSymbol = compilation.GetSymbol(memberSyntax);
//             var memberScope = memberSymbol switch
//             {
//                 ModuleSymbol moduleSymbol => new ModuleScope(parent: globalScope, moduleSymbol),
//                     ...
//             }
//         }
//     }
//
//     private static BinderContext GetMemberContext(Compilation compilation, BinderContext? parent, MemberSyntax syntax)
//     {
//         var memberSymbol = compilation.GetSymbol(syntax);
//         return memberSymbol switch
//         {
//             ModuleSymbol moduleSymbol => new BinderContext(parent, new ModuleScope(moduleSymbol)),
//             FnSymbol fnSymbol => new BinderContext(parent, new FnScope(fnSymbol)),
//         };
//     }
// }

// public record BinderContext(BinderContext? Parent, Scope Scope);
//
// public class Binder
// {
//     public static AxlType BindType(TypeNameSyntax syntax, BinderContext context)
//     {
//         var idNameSyntax = (IdNameSyntax)syntax;
//         var symbol = context.Scope.Lookup(SymbolName.From(idNameSyntax.Token));
//     }
// }
//
// public sealed record ModuleSymbol(Compilation Compilation, SymbolName Name, ModuleDeclSyntax Syntax) 
//     : Symbol(Compilation, Name)
// {
//     public ImmutableArray<Symbol> GetMembers()
//     {
//         return [.. Syntax.Members.Select(Compilation.GetSymbol)];
//     }
// }
//
// public sealed record TempPrintFnSymbol() : Symbol(SymbolName.From("Print"));
// public sealed record TempPrintLineFnSymbol() : Symbol(SymbolName.From("PrintLine"));
//
// public abstract class Scope
// {
//     public abstract Symbol? Lookup(SymbolName name);
//     public abstract List<Symbol> CollectAt(int position);
// }
//
// /// <summary>
// /// Mutable scope built as the binder walks the AST.
// /// </summary>
// public class LocalScope(Scope? parent = null) : Scope
// {
//     private readonly List<LocalSymbol> _locals = [];
//
//     public Scope? Parent { get; } = parent;
//
//     public override Symbol? Lookup(SymbolName name) // NON-EMPTY
//     {
//         // Search from last index, because shadowing might declare 
//         // multiple locals with the same name.
//         if (_locals.LastOrDefault(s => s.Name == name) is LocalSymbol symbol)
//             return symbol;
//         
//         return Parent?.Lookup(name);
//     }
//
//     public override List<Symbol> CollectAt(int position)
//     {
//         var collectedByName = new Dictionary<SymbolName, Symbol>();
//
//         for (var i = _locals.Count; i >= 0; i--)
//         {
//             var local = _locals[i];
//             if (local.Syntax.Span!.Value.End > position)
//                 continue;
//             collectedByName.TryAdd(local.Name, local);
//         }
//
//         if (Parent is null)
//             return [.. collectedByName.Values];
//
//         foreach (var symbol in Parent.CollectAt(position))
//         {
//             collectedByName.TryAdd(symbol.Name, symbol);
//         }
//         
//         return [.. collectedByName.Values];
//     }
//     
//     public void Declare(LocalSymbol symbol)
//     {
//         _locals.Add(symbol);
//     }
// }
//
// public class TempGlobalScope() : Scope
// {
//     private readonly TempPrintFnSymbol _print = new();
//     private readonly TempPrintLineFnSymbol _printLine = new();
//     
//     public override Symbol? Lookup(SymbolName name)
//     {
//         if (name == _print.Name)
//             return _print;
//         if (name == _printLine.Name)
//             return _printLine;
//
//         return null;
//     }
//
//     public override List<Symbol> CollectAt(int position)
//         => [_print, _printLine];
// }