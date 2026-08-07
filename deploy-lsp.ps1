$ErrorActionPreference = "Stop"

dotnet build "$PSScriptRoot\src\Axl\Axl.csproj" -c Debug
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$src = "$env:USERPROFILE\source\Axl\artifacts\bin\Axl\debug\*"
$dst = "$env:USERPROFILE\source\Axl\artifacts\lsp"
New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item -Path $src -Destination $dst -Recurse -Force