param([switch]$RequireSigned)
$ErrorActionPreference = 'Stop'
$version = '1.4.0'
$output = Join-Path $PSScriptRoot "dist\v$version"
$app = Join-Path $output 'QuotaDock.exe'
$setup = Join-Path $output "QuotaDock-$version-Setup.exe"
foreach ($file in @($app, $setup)) {
    $info = (Get-Item -LiteralPath $file).VersionInfo
    if ($info.ProductName -ne 'QuotaDock' -or $info.FileVersion -ne "$version.0" -or $info.ProductVersion -ne $version) { throw 'Artifact metadata does not match the release.' }
    if ($RequireSigned -and (Get-AuthenticodeSignature -LiteralPath $file).Status -ne 'Valid') { throw 'A trusted signature is required on both application and installer.' }
}
foreach ($name in @('README.md', 'CHANGELOG.md', 'PROVIDERS.md', 'LICENSE', 'PRIVACY.md')) { Copy-Item (Join-Path $PSScriptRoot $name) (Join-Path $output $name) -Force }
$images = Join-Path $output 'docs\images'
New-Item -ItemType Directory -Path $images -Force | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'docs\images\*.png') $images -Force
$zip = Join-Path $output "QuotaDock-$version-Windows-Portable.zip"
Compress-Archive -Path @($app, "$output\README.md", "$output\CHANGELOG.md", "$output\PROVIDERS.md", "$output\LICENSE", "$output\PRIVACY.md", "$output\docs") -DestinationPath $zip -Force
$hashes = foreach ($asset in @($setup, $zip, $app)) { '{0}  {1}' -f (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $asset -Leaf) }
$hashes | Set-Content -Path (Join-Path $output 'SHA256SUMS.txt') -Encoding ASCII
Write-Host "Artefactos v$version listos: $output"
