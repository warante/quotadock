$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$version = '1.0.0'
$output = Join-Path $PSScriptRoot "dist\v$version"
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$setup = Join-Path $output "QuotaDock-$version-Setup.exe"
& $compiler /nologo /target:winexe /optimize+ /utf8output /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll "/out:$setup" "/resource:$output\QuotaDock.OpenAI.exe,QuotaDock.OpenAI.exe" "/resource:$output\QuotaDock.Claude.exe,QuotaDock.Claude.exe" "/resource:$PSScriptRoot\Uninstall.ps1,Uninstall.ps1" "$PSScriptRoot\Installer.cs" "$PSScriptRoot\AssemblyInfo.cs"
if ($LASTEXITCODE -ne 0) { throw 'Error compilando instalador' }
Copy-Item (Join-Path $PSScriptRoot 'README.md') (Join-Path $output 'README.md') -Force
Copy-Item (Join-Path $PSScriptRoot 'CHANGELOG.md') (Join-Path $output 'CHANGELOG.md') -Force
$zip = Join-Path $output "QuotaDock-$version-Windows-Portable.zip"
Compress-Archive -Path @("$output\QuotaDock.OpenAI.exe", "$output\QuotaDock.Claude.exe", "$output\README.md", "$output\CHANGELOG.md") -DestinationPath $zip -Force
$assets = @($setup, $zip, "$output\QuotaDock.OpenAI.exe", "$output\QuotaDock.Claude.exe")
$hashes = foreach ($asset in $assets) { '{0}  {1}' -f (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $asset -Leaf) }
$hashes | Set-Content -Path (Join-Path $output 'SHA256SUMS.txt') -Encoding ASCII
Write-Host "Artefactos v$version listos: $output"
