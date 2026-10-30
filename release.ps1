param([switch]$RequireSigned, [switch]$UseExistingApp, [switch]$SkipLocalSigning)
$ErrorActionPreference = 'Stop'
if ($RequireSigned -and $SkipLocalSigning) { throw 'Use package.ps1 -RequireSigned after the external signing stages.' }
if (!$UseExistingApp) { & (Join-Path $PSScriptRoot 'build.ps1') }
$version = '1.4.0'
$output = Join-Path $PSScriptRoot "dist\v$version"
$appInfo = (Get-Item -LiteralPath "$output\QuotaDock.exe").VersionInfo
if ($appInfo.FileVersion -ne "$version.0" -or $appInfo.ProductName -ne 'QuotaDock' -or $appInfo.ProductVersion -ne $version) { throw 'Existing application metadata does not match this release.' }
if (!$SkipLocalSigning) { & (Join-Path $PSScriptRoot 'sign.ps1') -Files @("$output\QuotaDock.exe") -RequireSigned:$RequireSigned }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
# Windows PowerShell 5 reads UTF-8 scripts correctly when a BOM is present.
$script = Join-Path $output 'Uninstall.ps1'
[IO.File]::WriteAllText($script, [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Uninstall.ps1')), (New-Object Text.UTF8Encoding($true)))
$setup = Join-Path $output "QuotaDock-$version-Setup.exe"
$references = @('/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Security.dll', '/r:System.Web.Extensions.dll', '/r:Microsoft.CSharp.dll')
& $compiler /nologo /target:winexe /optimize+ /utf8output @references "/out:$setup" "/resource:$output\QuotaDock.exe,QuotaDock.exe" "/resource:$script,Uninstall.ps1" "/resource:$PSScriptRoot\LICENSE,LICENSE" "/resource:$PSScriptRoot\PRIVACY.md,PRIVACY.md" "$PSScriptRoot\Installer.cs" "$PSScriptRoot\Installation.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\Provider.cs" "$PSScriptRoot\Usage.cs" "$PSScriptRoot\ResetInfo.cs" "$PSScriptRoot\AssemblyInfo.cs" /r:System.Net.Http.dll
if ($LASTEXITCODE -ne 0) { throw 'Error compilando instalador' }
if (!$SkipLocalSigning) { & (Join-Path $PSScriptRoot 'sign.ps1') -Files @($setup) -RequireSigned:$RequireSigned }
& (Join-Path $PSScriptRoot 'package.ps1') -RequireSigned:$RequireSigned
