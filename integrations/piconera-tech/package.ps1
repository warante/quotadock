$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'test.ps1')
$output = Join-Path $PSScriptRoot '..\..\dist\integrations'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$zip = Join-Path $output 'QuotaDock-PiconeraTech-kit.zip'
Compress-Archive -Path (Join-Path $PSScriptRoot '*') -DestinationPath $zip -Force
('{0}  {1}' -f (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $zip -Leaf)) | Set-Content -LiteralPath (Join-Path $output 'QuotaDock-PiconeraTech-kit.sha256') -Encoding ASCII
Write-Host 'OK: kit ZIP de Piconera Tech y hash SHA-256 generados.'
