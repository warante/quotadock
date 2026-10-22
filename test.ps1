param([switch]$Live)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$output = Join-Path $PSScriptRoot 'dist\tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references = @('/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Net.Http.dll', '/r:System.Web.Extensions.dll')
foreach ($provider in @('OpenAI', 'Claude')) {
    $defines = if ($provider -eq 'Claude') { '/define:CLAUDE' } else { '/define:OPENAI' }
    $exe = Join-Path $output "$provider.Tests.exe"
    & $compiler /nologo /target:exe /main:Tests $defines @references "/out:$exe" "$PSScriptRoot\UsageTray.cs" "$PSScriptRoot\Provider.cs" "$PSScriptRoot\Tests.cs"
    if ($LASTEXITCODE -ne 0) { throw "No se compilaron las pruebas: $provider" }
    if ($Live) { & $exe --live } else { & $exe }
    if ($LASTEXITCODE -ne 0) { throw "Pruebas fallidas: $provider" }
}
