#!/usr/bin/env dotnet
#:project src/Axl.Compiler/Axl.Compiler.csproj
#:project src/Axl.Tests/Axl.Tests.csproj

using System.Buffers;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Taxl;
using Axl.Tests;


var input = """
            //@check
            var a: i32 = "Hello"; 
            """;

var source = SourceFileView.FromText(input);
var file = TaxlFile.Parse(source);
TaxlRunner.Test(file);