$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$output = Join-Path $PSScriptRoot 'dist\tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references = @('/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Net.Http.dll', '/r:System.Web.Extensions.dll', '/r:System.Security.dll')
$exe = Join-Path $output 'QuotaDock.Tests.exe'
& $compiler /nologo /target:exe /main:Tests @references "/out:$exe" "$PSScriptRoot\UsageTray.cs" "$PSScriptRoot\Ui.cs" "$PSScriptRoot\Usage.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\Provider.cs" "$PSScriptRoot\Tests.cs"
if ($LASTEXITCODE -ne 0) { throw 'No se compilaron las pruebas' }
$old = $env:OPENCODE_TEST_TEMP
try {
    $approved = Join-Path $env:TEMP 'opencode'
    if (Test-Path $approved) { $env:OPENCODE_TEST_TEMP = $approved }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Pruebas fallidas' }
} finally { $env:OPENCODE_TEST_TEMP = $old }
$installTests = Join-Path $output 'QuotaDock.InstallationTests.exe'
& $compiler /nologo /target:exe /main:InstallationTests @references /r:Microsoft.CSharp.dll "/out:$installTests" "$PSScriptRoot\Installation.cs" "$PSScriptRoot\InstallationTests.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\Provider.cs" "$PSScriptRoot\Usage.cs"
if ($LASTEXITCODE -ne 0) { throw 'No se compilaron las pruebas de instalación' }
& $installTests
if ($LASTEXITCODE -ne 0) { throw 'Pruebas de instalación fallidas' }
$visualTests = Join-Path $output 'QuotaDock.VisualTests.exe'
& $compiler /nologo /target:exe /main:VisualTests @references "/out:$visualTests" "$PSScriptRoot\VisualTests.cs" "$PSScriptRoot\UsageTray.cs" "$PSScriptRoot\Ui.cs" "$PSScriptRoot\Usage.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\Provider.cs"
if ($LASTEXITCODE -ne 0) { throw 'No se compilaron las pruebas visuales' }
& $visualTests
if ($LASTEXITCODE -ne 0) { throw 'Pruebas visuales fallidas' }
