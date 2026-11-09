$ErrorActionPreference = "Stop"

dotnet build "$PSScriptRoot\src\AxlLsp\AxlLsp.csproj" -c Debug
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$src = "$env:USERPROFILE\source\Axl\artifacts\bin\AxlLsp\debug"
$dst = "$env:USERPROFILE\source\Axl\artifacts\lsp"
New-Item -ItemType Directory -Force -Path $dst | Out-Null
Get-ChildItem -Path $src -Exclude "BaseLib" | Copy-Item -Destination $dst -Recurse -Force