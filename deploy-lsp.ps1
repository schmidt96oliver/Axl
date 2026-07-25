$src = "$env:USERPROFILE\source\Axl\artifacts\bin\Axl\debug\*"
$dst = "$env:USERPROFILE\source\Axl\artifacts\lsp"
New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item -Path $src -Destination $dst -Recurse -Force