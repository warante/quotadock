$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$output = Join-Path $PSScriptRoot 'dist\v1.1.0'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references = @('/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Net.Http.dll', '/r:System.Web.Extensions.dll', '/r:System.Security.dll')
& $compiler /nologo /target:winexe /optimize+ /utf8output @references "/out:$output\QuotaDock.exe" "$PSScriptRoot\UsageTray.cs" "$PSScriptRoot\Usage.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\Provider.cs" "$PSScriptRoot\AssemblyInfo.cs"
if ($LASTEXITCODE -ne 0) { throw 'Error de compilación' }
Write-Host "Creado: $output\QuotaDock.exe"
