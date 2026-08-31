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
/// Part of a module declaration declared by a single <see cref="BaseModuleDeclSyntax"/>.
/// </summary>
/// <param name="Name">Empty, if this fragment represents the entire file.</param>
/// <param name="Syntax">
/// <c>null</c> if compiler-generated declaration, i.e. those
/// fragments that represent the entire file or left parts of
/// paths. E.g. in `module A.B.C;`, A and B are compiler generated.
/// </param>
public sealed record ModuleDeclFragment(
    SymbolName Name,
    BaseModuleDeclSyntax? Syntax,
    ImmutableArray<ModuleDeclFragment> ChildFragments,
    ImmutableArray<Diagnostic> Diagnostics);

/// <summary>
/// A fully merged module declaration composed of many <see cref="ModuleDeclFragment"/>s.
/// </summary>
public sealed class ModuleDecl(SymbolName name, ImmutableArray<ModuleDeclFragment> fragments)
{
    /// <summary>
    /// Empty, if this is the global module.
    /// </summary>
    public SymbolName Name { get; } = name;

    public bool IsGlobal => Name.IsEmpty;
    
    
    public ImmutableArray<ModuleDecl> ChildModules
    {
        get
        {
            if (field.IsDefault)
            {
                field =
                [
                    .. fragments
                        .SelectMany(singleDecl => singleDecl.ChildFragments)
                        .GroupBy(singleDecl => singleDecl.Name)
                        .Select(grouping =>
                            new ModuleDecl(grouping.Key, [.. grouping]))
                ];
            }
            
            return field;
        }
    }

    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (field.IsDefault)
                field = [.. fragments.SelectMany(decl => decl.Diagnostics)];
            return field;
        }
    }

    /// <summary>
    /// Empty, if this is the global module.
    /// </summary>
    public ImmutableArray<BaseModuleDeclSyntax> Syntaxes
    {
        get
        {
            if (field.IsDefault)
            {
                field =
                [
                    .. fragments
                        .Select(fragment => fragment.Syntax)
                        .Where(syntax => syntax is not null)!
                ];
            }

            return field;
        }
    }
}

public sealed class DeclarationTable(ImmutableArray<SyntaxTree> trees)
{
    private readonly ImmutableArray<SyntaxTree> _trees = trees;
    private readonly Dictionary<SyntaxTree, ModuleDeclFragment?> _fileDeclsPerTree = [];

    public ModuleDecl GlobalDecl
    {
        get
        {
            field ??= new ModuleDecl(SymbolName.Empty, [
                .. _trees
                    .Select(GetFileFragment)
                    .Where(frag => frag is not null)!
            ]);
            return field;
        }
    }
    
    private ModuleDeclFragment? GetFileFragment(SyntaxTree tree)
    {
        if (_fileDeclsPerTree.TryGetValue(tree, out var fragment))
            return fragment;

        fragment = BuildFileFragment(tree);
        if (fragment is not null)
            _fileDeclsPerTree.Add(tree, fragment);
        return fragment;
    }

    private ModuleDeclFragment? BuildFileFragment(SyntaxTree tree)
    {
        // Usings are irrelevant for the declaration table.
        var declNodes = tree.FileSyntax
            .SyntaxNodes()
            .Where(node => node.Kind is not SyntaxKind.UsingDirective)
            .ToList();

        var isScriptFile = declNodes.Any(node => node is not BaseModuleDeclSyntax) || declNodes.Count == 0;
        if (isScriptFile)
            return null;

        var diagnosticBag = new DiagnosticBag();
        var fragments = ImmutableArray.CreateBuilder<ModuleDeclFragment>();

        // Build first fragment separately, so we can reject file-scoped decls
        // later in the loop. File-scoped decls must always be the first.
        Debug.Assert(declNodes.Count > 0);
        fragments.Add(BuildModuleDeclFragment((BaseModuleDeclSyntax)declNodes[0]));
        
        foreach (var node in declNodes.Skip(1))
        {
            Debug.Assert(node is BaseModuleDeclSyntax);

            if (node is FileScopedModuleDeclSyntax fileScopedSyntax)
                diagnosticBag.ReportError(new Diagnostic.InvalidFileScopedModuleDecl(fileScopedSyntax));
            
            fragments.Add(BuildModuleDeclFragment((BaseModuleDeclSyntax)node));
        }

        return new ModuleDeclFragment(SymbolName.Empty,
            Syntax: null,
            fragments.DrainToImmutable(),
            diagnosticBag.Drain());
    }
    
    private ModuleDeclFragment BuildModuleDeclFragment(BaseModuleDeclSyntax syntax)
    {
        // Report diagnostic for all file-scoped declarations.
        // The parser closes them immediately, so they do not
        // contain anything relevant. Do not add a declaration
        // for them.
        var diagnosticBag = new DiagnosticBag();
        foreach (var fileScopedDeclSyntax in syntax.Members.OfType<FileScopedModuleDeclSyntax>())
        {
            diagnosticBag.ReportError(new Diagnostic.InvalidFileScopedModuleDecl(fileScopedDeclSyntax));
        }
        
        // Visit all children module decls
        var childModuleDecls = syntax.Members
            .OfType<ModuleDeclSyntax>()
            .Select(BuildModuleDeclFragment)
            .ToImmutableArray();
        
        // Reduce dotted paths as in
        // `module A.B.C`
        var pathNameParts = syntax.Name.Parts.ToList();
        Debug.Assert(pathNameParts.Count >= 1, $"Parser must emit at least one part for {nameof(PathSyntax)}");

        var currentSyntax = syntax;
        for (var pathPart = pathNameParts.Count - 1; pathPart >= 1; pathPart--)
        {
            var decl = new ModuleDeclFragment(SymbolName.From(pathNameParts[pathPart]), 
                currentSyntax,
                childModuleDecls, 
                Diagnostics: diagnosticBag?.Drain() ?? []);
            
            childModuleDecls = [decl];
            currentSyntax = null;
            diagnosticBag = null;
        }
        
        return new ModuleDeclFragment(SymbolName.From(pathNameParts[0]), 
            currentSyntax, 
            childModuleDecls, 
            Diagnostics: diagnosticBag?.Drain() ?? []);
    }
}

public sealed record NewModuleSymbol(
    Compilation Compilation,
    ModuleDecl Decl,
    Symbol? Parent)
    : Symbol(Compilation, Decl.Name, Parent)
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
            var moduleMembers = Decl.ChildModules.Select(
                mergedDecl => new NewModuleSymbol(Compilation, mergedDecl, Parent: this));
            var otherMembers = Decl.Syntaxes
                .SelectMany(syntax => syntax.Members)
                .Where(syntax => syntax is not BaseModuleDeclSyntax)
                .Select(MakeSymbol);
            
            _members = [.. moduleMembers, .. otherMembers];
        }

        return _members;
    }


    public override ImmutableArray<SyntaxNode> GetDeclaringSyntaxes()
        => [.. Decl.Syntaxes];
}