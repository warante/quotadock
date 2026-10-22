$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$answer = [System.Windows.Forms.MessageBox]::Show('¿Desinstalar QuotaDock para este usuario? Se cerrarán sus aplicaciones instaladas. Las credenciales y las versiones portables no se modificarán.', 'QuotaDock', 'YesNo', 'Question')
if ($answer -ne 'Yes') { exit }
try {
    $expected = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'QuotaDock'
    if ([IO.Path]::GetFullPath($PSScriptRoot) -ne [IO.Path]::GetFullPath($expected)) { throw 'Ruta de instalación inesperada.' }
    foreach ($name in @('QuotaDock.OpenAI', 'QuotaDock.Claude')) {
        $target = Join-Path $PSScriptRoot "$name.exe"
        Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $target } | Stop-Process
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
        $startup = Join-Path ([Environment]::GetFolderPath('Startup')) "$name.lnk"
        if (Test-Path -LiteralPath $startup) { Remove-Item -LiteralPath $startup }
    }
    $programs = Join-Path ([Environment]::GetFolderPath('Programs')) 'QuotaDock'
    foreach ($name in @('QuotaDock - OpenAI.lnk', 'QuotaDock - Claude.lnk', 'Desinstalar QuotaDock.lnk')) {
        $link = Join-Path $programs $name
        if (Test-Path -LiteralPath $link) { Remove-Item -LiteralPath $link }
    }
    if ((Test-Path -LiteralPath $programs) -and @(Get-ChildItem -LiteralPath $programs -Force).Count -eq 0) { Remove-Item -LiteralPath $programs }
    $registry = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\QuotaDock'
    if (Test-Path $registry) { Remove-Item $registry }
    Remove-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall.ps1')
    if (@(Get-ChildItem -LiteralPath $PSScriptRoot -Force).Count -eq 0) { Remove-Item -LiteralPath $PSScriptRoot }
    [System.Windows.Forms.MessageBox]::Show('QuotaDock desinstalado.', 'QuotaDock') | Out-Null
} catch {
    [System.Windows.Forms.MessageBox]::Show('No se pudo completar la desinstalación. Cierra QuotaDock y vuelve a intentarlo.', 'QuotaDock') | Out-Null
    exit 1
}
