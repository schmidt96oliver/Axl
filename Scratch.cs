#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj

using System.Buffers;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Taxl;


var input = """
            //@run-pass
               //@check
             // This is an in-between comment
             //@run-panic
            //@bla

            Bla; // Hello
            //@blupp
                //--- Axl.test
            var a = 2 + 3; //~ error TypeMismatch
            //~lint
            //~type ^^^^^ i32
            //~type i32
            //~type   ^^^^^^^^^^^^ i32
            //~type ^

             //===stdout
            // Hello
            // World
            """;

var source = SourceFileView.FromText(input);
var file = TaxlFile.Parse(source);
Console.WriteLine($"Directives: {string.Join(", ", file.Directives.Select(dir => dir.Kind))}");
foreach (var part in file.Fragments)
{
    Console.WriteLine($"--- {part.GetType().Name} \"{part.Name}\"");
    if (part is TaxlFragment.Code codePart)
    {
        foreach (var annotation in codePart.Annotations)
        {
            Console.Write("--- ");
            switch (annotation)
            {
                case TaxlAnnotation.Diagnostic diagAnnotation:
                    Console.WriteLine($"{diagAnnotation.Kind}@l.{diagAnnotation.LineNumber}: {diagAnnotation.Id}");
                    break;
                case TaxlAnnotation.Type typeAnnotation:
                    var refText = source.GetText(typeAnnotation.ExprSpan);
                    Console.WriteLine($"type {typeAnnotation.TypeName} on \"{refText}\"");
                    break;
                case TaxlAnnotation.Invalid invalidAnnotation:
                    Console.WriteLine($"invalid: {invalidAnnotation.ErrorMessage}");
                    break;
            }
        }
    }

    Console.WriteLine(part.View.TextSpan);
}







