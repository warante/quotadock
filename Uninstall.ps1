param([switch]$Quiet, [string]$TestRoot)
$ErrorActionPreference = 'Stop'
$testing = ![string]::IsNullOrWhiteSpace($TestRoot)
if ($testing) {
    # The integration test operates only in its own sentinel-marked temporary directory.
    $expected = [IO.Path]::GetFullPath($TestRoot)
    if (!(Test-Path -LiteralPath (Join-Path $expected 'QuotaDock.InstallTest'))) { throw 'Missing test sentinel.' }
} else {
    $expected = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'QuotaDock'
}
if ([IO.Path]::GetFullPath($PSScriptRoot) -ne [IO.Path]::GetFullPath($expected)) { throw 'Unexpected installation path.' }
if (!$Quiet) {
    Add-Type -AssemblyName System.Windows.Forms
    $answer = [System.Windows.Forms.MessageBox]::Show('¿Desinstalar QuotaDock? Se cerrará la copia instalada. Las sesiones oficiales y las versiones portables no se modificarán.', 'QuotaDock', 'YesNo', 'Question')
    if ($answer -ne 'Yes') { exit }
}
try {
    $target = Join-Path $PSScriptRoot 'QuotaDock.exe'
    if (!$testing) {
        Get-Process -Name 'QuotaDock' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $target } | Stop-Process
        $run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
        $command = (Get-ItemProperty -Path $run -Name 'QuotaDock' -ErrorAction SilentlyContinue).QuotaDock
        if ($command -eq ('"' + $target + '"')) { Remove-ItemProperty -Path $run -Name 'QuotaDock' }
    }
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
    foreach ($name in @('LICENSE', 'PRIVACY.md')) {
        $document = Join-Path $PSScriptRoot $name
        if (Test-Path -LiteralPath $document) { Remove-Item -LiteralPath $document }
    }
    $programs = if ($testing) { Join-Path $PSScriptRoot 'Programs' } else { Join-Path ([Environment]::GetFolderPath('Programs')) 'QuotaDock' }
    foreach ($name in @('QuotaDock.lnk', 'Desinstalar QuotaDock.lnk')) {
        $link = Join-Path $programs $name
        if (Test-Path -LiteralPath $link) { Remove-Item -LiteralPath $link }
    }
    if ((Test-Path -LiteralPath $programs) -and @(Get-ChildItem -LiteralPath $programs -Force).Count -eq 0) { Remove-Item -LiteralPath $programs }
    if (!$testing) {
        $registration = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\QuotaDock'
        $installed = (Get-ItemProperty -Path $registration -Name 'InstallLocation' -ErrorAction SilentlyContinue).InstallLocation
        if ($installed -eq $expected) { Remove-Item -LiteralPath $registration }
    }
    # Deliberately preserve settings and encrypted credentials: never delete the official clients' stores.
    Remove-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall.ps1')
    if (@(Get-ChildItem -LiteralPath $PSScriptRoot -Force).Count -eq 0) { Remove-Item -LiteralPath $PSScriptRoot }
    if (!$Quiet) { [System.Windows.Forms.MessageBox]::Show('QuotaDock desinstalado. Tus ajustes y claves cifradas se conservan en %APPDATA%\QuotaDock. Usa «Olvidar clave» antes de desinstalar si quieres eliminarlas.', 'QuotaDock') | Out-Null }
} catch {
    if (!$Quiet) { [System.Windows.Forms.MessageBox]::Show('No se pudo completar la desinstalación. Cierra QuotaDock y vuelve a intentarlo.', 'QuotaDock') | Out-Null }
    exit 1
}
