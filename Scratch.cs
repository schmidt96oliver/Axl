#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Binders;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;


string[] inputs = ["""
                   module A
                   {
                        module C;
                       fn Fn_A() { }
                       module B 
                       {
                            fn Fn_B() { }
                       }
                   }
                   ""","""
                   module A.B;
                   
                            fn Fn2_B() { }
                       
                   
                   """,
];

var trees = inputs.Select(text => Parser.Parse(SourceFileView.FromText(text))).ToImmutableArray();
var compilation = Compilation.FromTrees(trees);
var declTable = new DeclarationTable(compilation.SyntaxTrees);

Console.WriteLine("*** SINGLE");
foreach (var tree in trees)
{
    var singleRoot = declTable.GetSyntaxTreeSingleRoot(tree);
    Console.WriteLine("----");
    PrintSingleDecl(singleRoot, "");
}

Console.WriteLine("*** MERGED");
PrintMergedDecl(declTable.GetMergedRoot(), string.Empty);

Console.WriteLine("*** Global Symbol");
var globalSymbol = new NewModuleSymbol(compilation, declTable.GetMergedRoot(), Parent: null);
PrintSymbol(globalSymbol, "");


void PrintSingleDecl(SingleModuleDecl decl, string prefix)
{
    var name = decl.Name.IsEmpty ? "ROOT" : decl.Name;
    var diagText = decl.Diagnostics.Length > 0 ? $"[ERRORx{decl.Diagnostics.Length}]" : "";
    var memberText = string.Join(" | ", decl.NonModuleMemberSyntaxes.Select(SelectName));
    
    Console.WriteLine($"{prefix}{name} {diagText} ({memberText})");
    foreach (var child in decl.Children) PrintSingleDecl(child, prefix + " - ");
}

void PrintMergedDecl(MergedModuleDecl decl, string prefix)
{
    var name = decl.Name.IsEmpty ? "ROOT" : decl.Name;
    var diagText = decl.Diagnostics.Length > 0 ? $"[ERRORx{decl.Diagnostics.Length}]" : "";
    var memberText = string.Join(" | ", decl.NonModuleMemberSyntaxes.Select(SelectName));
    
    Console.WriteLine($"{prefix}{name} {diagText} ({memberText})");
    foreach (var child in decl.Children) PrintMergedDecl(child, prefix + " ");

    
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



// --------- SingleModuleDecl Builder
/// <summary>
/// A single module declaration. Unmerged, even across the same file.
/// </summary>
/// <param name="Syntax">Declaring syntax.</param>
/// <param name="NonModuleMemberSyntaxes">
/// Syntax for all members that are not module decls.
/// Needs to be specially here, because file-scoped declarations
/// make all members below their own members.
/// </param>
/// <param name="Children">Child single module declarations.</param>
public sealed record SingleModuleDecl(SymbolName Name, 
    SyntaxNode Syntax,
    ImmutableArray<MemberSyntax> NonModuleMemberSyntaxes,    // needs to be given, because file-scoped is flat in syntax tree
    ImmutableArray<SingleModuleDecl> Children,
    ImmutableArray<Diagnostic> Diagnostics);
//TODO: Split root (empty name) from actual declarations?
//TODO: Can syntax tree actually be walked by scope tree builder?

public sealed class MergedModuleDecl(SymbolName name, ImmutableArray<SingleModuleDecl> singleModuleDecls)
{
    public SymbolName Name { get; } = name;
    
    public ImmutableArray<SingleModuleDecl> SingleModuleDecls { get; } = singleModuleDecls;

    public ImmutableArray<MergedModuleDecl> Children
    {
        get
        {
            if (field.IsDefault)
                field = MergeChildren();
            return field;
        }
    }

    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (field.IsDefault)
                field = [.. SingleModuleDecls.SelectMany(decl => decl.Diagnostics)];
            return field;
        }
    }

    public IEnumerable<MemberSyntax> NonModuleMemberSyntaxes
        => SingleModuleDecls.SelectMany(decl => decl.NonModuleMemberSyntaxes);

    private ImmutableArray<MergedModuleDecl> MergeChildren()
    {
        var singleChildren = SingleModuleDecls.SelectMany(singleDecl => singleDecl.Children);

        var groupsByName = singleChildren.GroupBy(singleDecl => singleDecl.Name);

        var mergedChildren = groupsByName.Select(grp =>
            new MergedModuleDecl(grp.Key, grp.ToImmutableArray())).ToImmutableArray();

        return mergedChildren;
    }
}

public sealed class DeclarationBuilder
{
    public static SingleModuleDecl Build(SyntaxTree tree)
    {
        var firstMember = tree.FileSyntax.Members.FirstOrDefault();
        var builder = new DeclarationBuilder();
        
        switch (firstMember)
        {
            case FileScopedModuleDeclSyntax fileScopedModuleDecl:
            {
                var members = tree.FileSyntax.Members
                    .Skip(1)
                    .ToImmutableArray();
                var decl = builder.VisitBaseModuleDecl(fileScopedModuleDecl,
                    members, new DiagnosticBag());

                return new SingleModuleDecl(SymbolName.Empty,
                    Syntax: tree.FileSyntax,
                    NonModuleMemberSyntaxes: [], // members are now part of the file-scoped decl
                    Children: [decl],
                    Diagnostics: []);
            }
            case ModuleDeclSyntax moduleDecl:
            {
                var children = tree.FileSyntax.Members
                    .OfType<BaseModuleDeclSyntax>()
                    .Select(builder.VisitModuleDecl)
                    .ToImmutableArray();
                
                return new SingleModuleDecl(SymbolName.Empty,
                    Syntax: tree.FileSyntax,
                    NonModuleMemberSyntaxes: [],
                    Children: children,
                    Diagnostics: []);
            }
            default:
                // Something else entirely, means we have a script file.
                // Might also be null (no member at all): Script file as well (?)
                //TODO: Visit script file

                return new SingleModuleDecl(
                    SymbolName.Empty,
                    Syntax: tree.FileSyntax,
                    NonModuleMemberSyntaxes: [],
                    Children: [],
                    Diagnostics: []);
        }
    }

    private SingleModuleDecl VisitBaseModuleDecl(BaseModuleDeclSyntax syntax, ImmutableArray<MemberSyntax> members, DiagnosticBag diagnostics)
    {
        // Visit all children module decls
        var childrenModuleDecls = members
            .OfType<BaseModuleDeclSyntax>()
            .Select(VisitModuleDecl)
            .ToImmutableArray();
        var nonModuleMemberSyntaxes = members
            .Where(s => s is not BaseModuleDeclSyntax)
            .ToImmutableArray();
        
        // Reduce dotted paths as in
        // `module A.B.C`
        var pathNameParts = syntax.Name.Parts.ToList();
        Debug.Assert(pathNameParts.Count >= 1, $"Parser must emit at least one part for {nameof(PathSyntax)}");
        
        for (var pathPart = pathNameParts.Count - 1; pathPart >= 1; pathPart--)
        {
            var decl = new SingleModuleDecl(SymbolName.From(pathNameParts[pathPart]), 
                syntax,
                nonModuleMemberSyntaxes,
                childrenModuleDecls, 
                Diagnostics: []);
            
            childrenModuleDecls = [decl];
            nonModuleMemberSyntaxes = [];
        }
        
        return new SingleModuleDecl(SymbolName.From(pathNameParts[0]), 
            syntax, 
            nonModuleMemberSyntaxes, 
            childrenModuleDecls, 
            diagnostics.Drain());
    }

    private SingleModuleDecl VisitModuleDecl(BaseModuleDeclSyntax syntax)
    {
        var diagnostics = new DiagnosticBag();
        
        // File-scoped declaration is illegal here, since the valid case is handled
        // special-cased. Emit a single declaration for it without children and
        // with an error attached.
        
        if (syntax is FileScopedModuleDeclSyntax)
        {
            //TODO: Report doubles or misplaced file-scoped module decl error
            diagnostics.ReportError(new Diagnostic.UnsupportedFeature(syntax));
        }

        return VisitBaseModuleDecl(syntax, 
            members: syntax is ModuleDeclSyntax moduleDeclSyntax ? [.. moduleDeclSyntax.Members] : [],
            diagnostics);
    }
}

public sealed class DeclarationTable(ImmutableArray<SyntaxTree> trees)
{
    private readonly ImmutableArray<SyntaxTree> _trees = trees;
    private readonly Dictionary<SyntaxTree, SingleModuleDecl> _rootPerTree = [];

    private MergedModuleDecl? _lazyMergedRoot = null; 
    
    
    public SingleModuleDecl GetSyntaxTreeSingleRoot(SyntaxTree tree)
    {
        if (_rootPerTree.TryGetValue(tree, out var root))
            return root;

        root = DeclarationBuilder.Build(tree);
        _rootPerTree.Add(tree, root);
        return root;
    }

    public MergedModuleDecl GetMergedRoot()
    {
        _lazyMergedRoot ??= MakeMergedRoot();
            
        return _lazyMergedRoot;
    }

    private MergedModuleDecl MakeMergedRoot()
    {
        var globalSingleDecls = _trees.Select(GetSyntaxTreeSingleRoot).ToImmutableArray();
        Debug.Assert(globalSingleDecls.All(root => root.Name.IsEmpty));

        return new MergedModuleDecl(SymbolName.Empty,
            singleModuleDecls: globalSingleDecls);
    }
}

public sealed record NewModuleSymbol(
    Compilation Compilation,
    MergedModuleDecl MergedDecl,
    Symbol? Parent)
    : Symbol(Compilation, MergedDecl.Name, Parent)
{
    private ImmutableArray<Symbol> _members = default;

    private Symbol MakeSymbol(MemberSyntax syntax) => syntax switch
    {
        FnDeclSyntax fnDecl => new FnSymbol(Compilation, SymbolName.From(fnDecl.Name),
            fnDecl, Parent: this),
        _ => throw new UnreachableException()
    };
    
    public ImmutableArray<Symbol> GetMembers()
    {
        if (_members.IsDefault)
        {
            // Create Module Symbols
            var moduleMembers = MergedDecl.Children.Select(
                mergedDecl => new NewModuleSymbol(Compilation, mergedDecl, Parent: this));
            var otherMembers = MergedDecl.NonModuleMemberSyntaxes.Select(MakeSymbol);
            
            _members = [.. moduleMembers, .. otherMembers];
        }

        return _members;
    }


    public override ImmutableArray<SyntaxNode> GetDeclaringSyntaxes()
        => [.. MergedDecl.SingleModuleDecls.Select(single => single.Syntax)];
}