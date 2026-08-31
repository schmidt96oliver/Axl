// using System.Collections.Frozen;
// using Axl.Compiler.Semantics.Symbols;
// using Axl.Compiler.Syntax;
// using Axl.Compiler.Syntax.Tree;
//
// namespace Axl.Compiler.Semantics.Binders;
//
// public sealed class _BinderFactory
// {
//     private readonly Compilation _compilation;
//     private FrozenDictionary<SyntaxNode, _Binder> _binderByNode;
//
//     private _BinderFactory(Compilation compilation, FrozenDictionary<SyntaxNode, _Binder> binderByNode)
//     {
//         _compilation = compilation;
//         _binderByNode = binderByNode;
//     }
//     
//     public static _BinderFactory Build(Compilation compilation)
//     {
//         var binders = new Dictionary<SyntaxNode, _Binder>();
//         var compilationBinder = new CompilationBinder(compilation, compilation.GetSymbolTable());
//         foreach (var syntaxTree in compilation.SyntaxTrees)
//             VisitSyntaxTree(syntaxTree, compilationBinder, compilation, binders);
//
//         return new _BinderFactory(compilation, binders.ToFrozenDictionary());
//     }
//
//     private static void VisitSyntaxTree(SyntaxTree syntaxTree, CompilationBinder parent, Compilation compilation,
//         Dictionary<SyntaxNode, _Binder> binders)
//     {
//         var fileBinder = new FileBinder(parent, syntaxTree);
//         foreach (var member in syntaxTree.FileSyntax.Members)
//         {
//             binders.Add(member, fileBinder);
//             if (member is ModuleDeclSyntax memberModuleSyntax)
//                 VisitModuleDecl(memberModuleSyntax, fileBinder, compilation, binders);
//         }
//     }
//
//     private static void VisitModuleDecl(ModuleDeclSyntax syntax, _Binder parent, Compilation compilation, Dictionary<SyntaxNode, _Binder> binders)
//     {
//         var moduleSymbol = (ModuleSymbol)compilation.GetSymbolTable().GetSymbol(syntax);
//         var moduleBinder = new ModuleFragmentBinder(parent, moduleSymbol, syntax);
//
//         foreach (var member in syntax.Members)
//         {
//             binders.Add(member, moduleBinder);
//             if (member is ModuleDeclSyntax memberModuleSyntax)
//                 VisitModuleDecl(memberModuleSyntax, moduleBinder, compilation, binders);
//         }
//     }
//     
//     
//     public _Binder GetBinderAt(SyntaxNode node)
//     {
//         return _binderByNode[node];
//     }
// }