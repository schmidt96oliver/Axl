using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Binders;

public abstract record HirNode;

public abstract record HirExpr(AxlType Type) : HirNode;

public sealed record HirNumberLiteralExpr(NumberLiteralToken Token, AxlType Type) : HirExpr(Type);

public sealed record HirLoopExpr(HirBody Body, AxlType Type) : HirExpr(Type);

public sealed record HirLocalRef(LocalSymbol Symbol, AxlType Type) : HirExpr(Type);

public sealed record HirBreakExpr(HirExpr? Expr, AxlType Type) : HirExpr(Type);

public sealed record HirCallDirect(Symbol FnSymbol, ImmutableArray<HirExpr> Arguments, AxlType Type) : HirExpr(Type);

public abstract record HirStmt : HirNode;

public sealed record HirExprStmt(HirExpr Expr) : HirStmt;
public sealed record HirVarDecl(LocalSymbol Local, HirExpr Initializer) : HirStmt;

public record HirBody(ImmutableArray<HirStmt> Stmts) : HirNode;


public abstract class Binder
{
    public Binder? Parent { get; }

    protected virtual Compilation Compilation { get; }


    protected Binder(Binder parent)
    {
        Parent = parent;
        Compilation = parent.Compilation;
    }

    protected Binder(Compilation compilation)
    {
        Parent = null;
        Compilation = compilation;
    }

    public abstract Symbol? Lookup(SymbolName name);
    

    public virtual AxlType BindType(TypeNameSyntax syntax)
    {
        if (syntax is not NativeTypeNameSyntax nativeTypeNameSyntax)
            throw new Exception("Only native types supported");

        switch (nativeTypeNameSyntax.Token.Kind)
        {
            case TokenKind.I32Kw: return Compilation.TypeContext.I32;
            case TokenKind.I64Kw: return Compilation.TypeContext.I64;
            case TokenKind.F32Kw: return Compilation.TypeContext.F32;
            case TokenKind.F64Kw: return Compilation.TypeContext.F64;
            case TokenKind.BoolKw: return Compilation.TypeContext.Bool;
            case TokenKind.StringKw: return Compilation.TypeContext.String;
            case TokenKind.NoneKw: return Compilation.TypeContext.None;
            case TokenKind.NeverKw: return Compilation.TypeContext.Never;
            
            default: throw new Exception("Unkown type name");
        }
    }

    
    public virtual HirBody BindBody(SyntaxNode node)
    {
        var boundStmts = ImmutableArray.CreateBuilder<HirStmt>();
        var localBinder = new LocalBinder(this);
        
        var stmtSyntaxes = node switch
        {
            BlockExprSyntax block => block.Stmts,
            FileSyntax file => file.Stmts,
            _ => throw new NotImplementedException()
        };

        foreach (var stmtSyntax in stmtSyntaxes)
            boundStmts.Add(localBinder.BindStmt(stmtSyntax));

        return new HirBody(boundStmts.DrainToImmutable());
    }
    
    
}

public sealed class LocalBinder : Binder
{
    private List<LocalSymbol> _locals = [];
    
    
    public LocalBinder(Binder parent) : base(parent)
    {
    }

    public override Symbol? Lookup(SymbolName name)
    {
        for (var i = _locals.Count - 1; i >= 0; i--)
        {
            if (_locals[i].Name == name)
                return _locals[i];
        }

        return Parent?.Lookup(name);
    }
    
    private void DeclareLocal(LocalSymbol local)
        => _locals.Add(local);
    
    
    public HirStmt BindStmt(SyntaxNode node) => node switch
    {
        ExprStmtSyntax exprStmt => new HirExprStmt(BindExpr(exprStmt.Expr)),
        VarDeclSyntax varDecl => BindVarDecl(varDecl),
        
        _ => throw new NotImplementedException()
    };

    private HirVarDecl BindVarDecl(VarDeclSyntax syntax)
    {
        //TODO: Handle absent initializer
        if (syntax.Name.IsMissing)
            return null; //TODO: Don't declare, but how?
        
        var name = SymbolName.From(syntax.Name.Identifier);
        
        var boundInitializer = BindExpr(syntax.Initializer!);
        var local = new LocalSymbol(Compilation, 
            name, boundInitializer.Type, syntax, null);
        
        DeclareLocal(local);
        return new HirVarDecl(local, boundInitializer);
    }

    private HirExpr BindExpr(ExprSyntax syntax) => syntax switch
    {
        NumberLiteralSyntax numberLiteral => BindNumberLiteral(numberLiteral),
        IdNameSyntax idName => BindIdName(idName),
        CallExprSyntax callExpr => BindCall(callExpr),
        
        _ => throw new NotImplementedException()
    };

    private HirCallDirect BindCall(CallExprSyntax syntax)
    {
        if (syntax.Callee is not IdNameSyntax idNameCallee)
            throw new Exception("Must be name.");

        var symbol = Lookup(SymbolName.From(idNameCallee))
                ?? throw new Exception("UndefinedSymbol");
        var args = syntax.ArgumentExprs.Select(argSyntax => BindExpr(argSyntax));
        return new HirCallDirect(symbol, [.. args], Compilation.TypeContext.None);
    }

    private HirExpr BindNumberLiteral(NumberLiteralSyntax numberLiteral)
    {
        AxlType type = numberLiteral.Token.Suffix switch
        {
            NumberLiteralSuffix.I32 => Compilation.TypeContext.I32,
            NumberLiteralSuffix.I64 => Compilation.TypeContext.I64,
            NumberLiteralSuffix.None => numberLiteral.Token.HasDecimalPoint
                ? throw new NotImplementedException()
                : Compilation.TypeContext.I32,

            _ => throw new NotImplementedException()
        };
        return new HirNumberLiteralExpr(numberLiteral.Token, type);
    }

    
    private HirLocalRef BindIdName(IdNameSyntax syntax)
    {
        var symbol = Lookup(SymbolName.From(syntax.Token.Identifier));
        if (symbol is null)
            throw new Exception("UndefinedSymbol");

        var local = (LocalSymbol)symbol;

        return new HirLocalRef(local, local.Type);
    }
}

public sealed class CompilationBinder : Binder
{
    private readonly SymbolTable _symbolTable;

    public CompilationBinder(Compilation compilation, SymbolTable symbolTable)
        : base(compilation)
    {
        _symbolTable = symbolTable;
    }

    public override Symbol? Lookup(SymbolName name)
    {
        return _symbolTable.AllSymbols.FirstOrDefault(symbol => symbol.Name == name);
    }
}

public sealed class FileBinder : Binder
{ 
    public SyntaxTree SyntaxTree { get; }
    
    public FileBinder(Binder parent, SyntaxTree syntaxTree)
        : base(parent)
    {
        SyntaxTree = syntaxTree;
    }

    public override Symbol? Lookup(SymbolName name)
    {
        //TODO: Build usings and return those
        return Parent?.Lookup(name);
    }

    
}

public sealed class FnBinder : Binder
{
    public FnSymbol FnSymbol { get; }

    public FnBinder(Binder parent, FnSymbol fnSymbol)
        : base(parent)
    {
        FnSymbol = fnSymbol;
    }

    public override Symbol? Lookup(SymbolName name)
    {
        return FnSymbol.GetParameters().FirstOrDefault(param => param.Name == name)
               ?? Parent?.Lookup(name);
    }
}

public sealed class ModuleFragmentBinder : Binder
{
    public ModuleSymbol ModuleSymbol { get; }
    
    private readonly ModuleDeclSyntax _moduleDeclSyntax;

    public ModuleFragmentBinder(Binder parent, ModuleSymbol moduleSymbol, ModuleDeclSyntax moduleDeclSyntax)
        : base(parent)
    {
        ModuleSymbol = moduleSymbol;
        _moduleDeclSyntax = moduleDeclSyntax;
    }

    public override Symbol? Lookup(SymbolName name)
    {
        //TODO: Build usings from decl syntax
        
        var members = ModuleSymbol.GetMembers();
        return members.FirstOrDefault(member => member.Name == name)
            ?? Parent?.Lookup(name);
    }
}