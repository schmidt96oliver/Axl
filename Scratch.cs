#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;


var input = """
            module A
            {
                module B
                {
                    fn Test1() { }
                    fn Test2() { }
                    module C { }
                }
            }
            module A
            {
                module B
                {
                    fn Test2() { }
                    module C { }
                }
                module C { }
                module A { }
            }
            module B { }
            module A.B.C { }
            module Some.Other.D { }
            """;

var source = SourceFileView.FromText(input);
var compilation = Compilation.FromSource(source);
var table = Declarator.GetSymbolTable(compilation, [compilation.GetSyntaxTree(compilation.GetFileId(source))]);
foreach (var symbol in table.AllSymbols)
{
    Console.Write($"{symbol.GetType().Name} {symbol.Name}: Parent = {symbol.Parent?.Name ?? "<null>"}");
    if (symbol is ModuleSymbol module)
        Console.WriteLine($", SyntaxCount = {module.Syntaxes.Length}");
    else
    {
        Console.WriteLine();
    }
}





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



// ----- LOCAL Binder

public abstract record HirNode;

public abstract record HirExpr(AxlType Type) : HirNode;

public sealed record HirNumberLiteralExpr(NumberLiteralToken Token, AxlType Type) : HirExpr(Type);

public sealed record HirLoopExpr(HirBody Body, AxlType Type) : HirExpr(Type);

public sealed record HirSymbolRef(LocalSymbol Symbol, AxlType Type) : HirExpr(Type);

public sealed record HirBreakExpr(HirExpr? Expr, AxlType Type) : HirExpr(Type);

public sealed record HirCallDirect(Symbol FnSymbol, ImmutableArray<HirExpr> Arguments, AxlType Type) : HirExpr(Type);

public abstract record HirStmt : HirNode;

public sealed record HirExprStmt(HirExpr Expr) : HirStmt;
public sealed record HirVarDecl(LocalSymbol Local, HirExpr Initializer) : HirStmt;

public record HirBody(ImmutableArray<HirStmt> Stmts) : HirNode;

public sealed record LoopContext(List<HirBreakExpr>? BreakExprs);
//
// //TODO: SyntaxNode, BodySyntax. Needs to include: fn body & file syntax
// public class BodyBinder(TypeContext typeContext)
// {
//     private ImmutableArray<HirStmt>.Builder _stmts = ImmutableArray.CreateBuilder<HirStmt>();
//
//     private HirBody BindBody(SyntaxNode node, Scope scope, LoopContext loopContext)
//     {
//         var bodyScope = new LocalScope(scope);
//         
//         var boundStmts = ImmutableArray.CreateBuilder<HirStmt>();
//
//         var stmtSyntaxes = node switch
//         {
//             BlockExprSyntax block => block.Stmts,
//             FileSyntax file => file.Stmts,
//             _ => throw new NotImplementedException()
//         };
//
//         foreach (var stmtSyntax in stmtSyntaxes)
//             boundStmts.Add(BindStmt(stmtSyntax, bodyScope, loopContext));
//
//         return new HirBody(boundStmts.DrainToImmutable());
//     }
//     
//     private HirStmt BindStmt(SyntaxNode node, LocalScope scope, LoopContext loopContext) => node switch
//     {
//         ExprStmtSyntax exprStmt => new HirExprStmt(BindExpr(exprStmt.Expr, scope, loopContext)),
//         VarDeclSyntax varDecl => BindVarDecl(varDecl, scope, loopContext),
//         
//         _ => throw new NotImplementedException()
//     };
//
//     private HirVarDecl BindVarDecl(VarDeclSyntax syntax, LocalScope scope, LoopContext loopContext)
//     {
//         //TODO: Handle absent initializer
//         if (syntax.Name.IsMissing)
//             return null; //TODO: Don't declare, but how?
//         
//         var name = SymbolName.From(syntax.Name.Identifier);
//         
//         var boundInitializer = BindExpr(syntax.Initializer!, scope, loopContext);
//         var local = new LocalSymbol(name, boundInitializer.Type, syntax);
//         
//         scope.Declare(local);
//         return new HirVarDecl(local, boundInitializer);
//     }
//     
//     
//     
//     public static HirBody Bind(SyntaxNode syntax, Scope enclosingScope, TypeContext typeContext)
//     {
//         return new BodyBinder(typeContext).BindBody(syntax, enclosingScope, new LoopContext(null));
//     }
//
//
//
//     private HirExpr BindExpr(ExprSyntax syntax, Scope scope, LoopContext loopContext) => syntax switch
//     {
//         NumberLiteralSyntax numberLiteral => BindNumberLiteral(numberLiteral, scope, loopContext),
//         IdNameSyntax idName => BindIdName(idName, scope, loopContext),
//         LoopExprSyntax loop => BindLoop(loop, scope, loopContext),
//         BreakExprSyntax breakExpr => BindBreak(breakExpr, scope, loopContext),
//         CallExprSyntax callExpr => BindCall(callExpr, scope, loopContext),
//         
//         _ => throw new NotImplementedException()
//     };
//
//     private HirCallDirect BindCall(CallExprSyntax syntax, Scope scope, LoopContext loopContext)
//     {
//         if (syntax.Callee is not IdNameSyntax idNameCallee)
//             throw new Exception("Must be name.");
//
//         var symbol = scope.Lookup(SymbolName.From(idNameCallee))
//                 ?? throw new Exception("UndefinedSymbol");
//         var args = syntax.ArgumentExprs.Select(argSyntax => BindExpr(argSyntax, scope, loopContext));
//         return new HirCallDirect(symbol, [.. args], typeContext.None);
//     }
//
//     private HirBreakExpr BindBreak(BreakExprSyntax syntax, Scope scope, LoopContext loopContext)
//     {
//         var expr = syntax.Expr is not null ? BindExpr(syntax.Expr, scope, loopContext) : null;
//         var boundBreak = new HirBreakExpr(expr, typeContext.Never);
//         loopContext.BreakExprs?.Add(boundBreak);
//         return boundBreak;
//     }
//
//     private HirExpr BindNumberLiteral(NumberLiteralSyntax numberLiteral, Scope scope, LoopContext loopContext)
//     {
//         AxlType type = numberLiteral.Token.Suffix switch
//         {
//             NumberLiteralSuffix.I32 => typeContext.I32,
//             NumberLiteralSuffix.I64 => typeContext.I64,
//             NumberLiteralSuffix.None => numberLiteral.Token.HasDecimalPoint
//                 ? throw new NotImplementedException()
//                 : typeContext.I32,
//
//             _ => throw new NotImplementedException()
//         };
//         return new HirNumberLiteralExpr(numberLiteral.Token, type);
//     }
//
//     private HirLoopExpr BindLoop(LoopExprSyntax syntax, Scope scope, LoopContext _)
//     {
//         var loopContext = new LoopContext([]);
//         var loopBody = BindBody(syntax.Body, scope, loopContext);
//
//         var type = loopContext.BreakExprs!.Count == 0
//             ? typeContext.Never
//             : loopContext.BreakExprs[0].Expr?.Type ?? typeContext.None;
//         return new HirLoopExpr(loopBody, type);
//     }
//     
//     private HirSymbolRef BindIdName(IdNameSyntax syntax, Scope scope, LoopContext loopContext)
//     {
//         var symbol = scope.Lookup(SymbolName.From(syntax.Token.Identifier));
//         if (symbol is null)
//             throw new Exception("UndefinedSymbol");
//
//         var local = (LocalSymbol)symbol;
//
//         return new HirSymbolRef(local, local.Type);
//     }
// }