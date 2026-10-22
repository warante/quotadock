param([switch]$RequireSigned)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$version = '1.1.0'
$output = Join-Path $PSScriptRoot "dist\v$version"
& (Join-Path $PSScriptRoot 'sign.ps1') -Files @("$output\QuotaDock.exe") -RequireSigned:$RequireSigned
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
# Windows PowerShell 5 reads UTF-8 scripts correctly when a BOM is present.
$script = Join-Path $output 'Uninstall.ps1'
[IO.File]::WriteAllText($script, [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Uninstall.ps1')), (New-Object Text.UTF8Encoding($true)))
$setup = Join-Path $output "QuotaDock-$version-Setup.exe"
$references = @('/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Security.dll', '/r:System.Web.Extensions.dll', '/r:Microsoft.CSharp.dll')
& $compiler /nologo /target:winexe /optimize+ /utf8output @references "/out:$setup" "/resource:$output\QuotaDock.exe,QuotaDock.exe" "/resource:$script,Uninstall.ps1" "$PSScriptRoot\Installer.cs" "$PSScriptRoot\Installation.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\Provider.cs" "$PSScriptRoot\Usage.cs" "$PSScriptRoot\AssemblyInfo.cs" /r:System.Net.Http.dll
if ($LASTEXITCODE -ne 0) { throw 'Error compilando instalador' }
& (Join-Path $PSScriptRoot 'sign.ps1') -Files @($setup) -RequireSigned:$RequireSigned
foreach ($name in @('README.md', 'CHANGELOG.md', 'PROVIDERS.md')) { Copy-Item (Join-Path $PSScriptRoot $name) (Join-Path $output $name) -Force }
$zip = Join-Path $output "QuotaDock-$version-Windows-Portable.zip"
Compress-Archive -Path @("$output\QuotaDock.exe", "$output\README.md", "$output\CHANGELOG.md", "$output\PROVIDERS.md") -DestinationPath $zip -Force
$assets = @($setup, $zip, "$output\QuotaDock.exe")
$hashes = foreach ($asset in $assets) { '{0}  {1}' -f (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $asset -Leaf) }
$hashes | Set-Content -Path (Join-Path $output 'SHA256SUMS.txt') -Encoding ASCII
Write-Host "Artefactos v$version listos: $output"
