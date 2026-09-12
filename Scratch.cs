#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj
#:project src/Axl.Tests/Axl.Tests.csproj

using Axl.Compiler.Testing;
using Axl.Compiler.Text;


var input = """
            //@check
            var a: i32 = 1.1;
            //~type       ^^ i32
            """;

var sourceText = SourceText.From(input);
var file = TestFile.From(sourceText);
var eval = file.Evaluate();

Console.WriteLine(eval.HasUnsupportedFeatures ? "Unsupported" : eval.HasFailed ? "Failed" : "Pass");
Console.WriteLine(eval.Message);