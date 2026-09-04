using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Semantics.Symbols;

public sealed class FnSymbol(
    Compilation compilation,
    SymbolName name,
    FnDeclSyntax syntax,
    Symbol? parent)
    : Symbol(compilation, name, parent)
{
    private readonly DiagnosticBag _diagnosticBag = new();
    private LazyField<ImmutableArray<LocalSymbol>> _lazyParameters;
    
    
    public FnDeclSyntax Syntax { get; } = syntax;

    public override ImmutableArray<SyntaxNode> DeclaringSyntaxes
    {
        get
        {
            if (field.IsDefault)
                field = [Syntax];
            return field;
        }
    }

    public ImmutableArray<LocalSymbol> ParameterSymbols
        => _lazyParameters.GetOrCreate(CreateParameterSymbols);

    public override string DisplayName => $"fn {Name}";


    public ImmutableArray<LocalSymbol> CreateParameterSymbols()
    {
        var parameterSymbols = Syntax.Parameters
            .Select(BindParameter)
            .Where(boundParameter => boundParameter is not null)
            .ToImmutableArray();
        ReportDuplicateParameterSymbols(parameterSymbols!);
        return parameterSymbols!;
    }

    private void ReportDuplicateParameterSymbols(ImmutableArray<LocalSymbol> parameterSymbols)
    {
        var groupsByName = parameterSymbols
            .GroupBy(local => local.Name);
        foreach (var group in groupsByName)
        {
            var localsWithSameName = group.ToImmutableArray();
            if (localsWithSameName.Length > 1)
                _diagnosticBag.ReportError(new Diagnostic.DuplicateParameters(localsWithSameName));
        }
    }

    private LocalSymbol? BindParameter(ParamSyntax syntax)
    {
        if (syntax.Name.IsMissing)
        {
            // Bind the type for diagnostics.
            // Do not report more diagnostics, in case
            // it is absent. A "missing identifier" error
            // is enough in that case.
            if (syntax.TypeAnnotation is not null)
                BindParameterType(syntax);
            return null;
        }

        var type = BindParameterType(syntax);
        return new LocalSymbol(Compilation,
            SymbolName.From(syntax.Name),
            type,
            syntax,
            parent: this);
    }

    /// <summary>
    /// Tries to bind a type name without context and returns <c>null</c> if it
    /// can't.
    /// </summary>
    private AxlType? BindTypeQuick(TypeNameSyntax syntax) => syntax switch
    {
        NativeTypeNameSyntax nativeTypeNameSyntax => nativeTypeNameSyntax.Token.Kind switch
        {
            TokenKind.I32Kw => Compilation.TypeContext.I32,
            TokenKind.I64Kw => Compilation.TypeContext.I64,
            TokenKind.F32Kw => Compilation.TypeContext.F32,
            TokenKind.F64Kw => Compilation.TypeContext.F64,
            TokenKind.BoolKw => Compilation.TypeContext.Bool,
            TokenKind.StringKw => Compilation.TypeContext.String,
            TokenKind.NoneKw => Compilation.TypeContext.None,
            _ => throw new UnreachableException("Invalid native type name token.")
        },
        _ => null
    };

    private AxlType BindParameterType(ParamSyntax syntax)
    {
        if (syntax.TypeAnnotation is null)
        {
            _diagnosticBag.ReportError(new Diagnostic.MissingParameterTypeAnnotation(syntax));
            return Compilation.TypeContext.Error;
        }

        var quickType = BindTypeQuick(syntax.TypeAnnotation);
        if (quickType is not null)
            return quickType;

        if (syntax.TypeAnnotation.Span is { IsEmpty: false })
            _diagnosticBag.ReportError(new Diagnostic.UnsupportedFeature(
                syntax.TypeAnnotation,
                "Only native types are supported for now."));

        return Compilation.TypeContext.Error;
    }


    public override void CollectDiagnosticsInto(DiagnosticBag diagnosticBag)
    {
        var parameters = ParameterSymbols;
        
        _diagnosticBag.DrainInto(diagnosticBag);
    }
}