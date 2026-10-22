$ErrorActionPreference = 'Stop'
$release = Join-Path $PSScriptRoot 'dist\v1.1.0'
$assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $release 'QuotaDock-1.1.0-Setup.exe'))
foreach ($name in @('QuotaDock.exe', 'Uninstall.ps1')) {
    if ($assembly.GetManifestResourceNames() -notcontains $name) { throw "Missing installer resource: $name" }
}
if ((Get-Item (Join-Path $release 'QuotaDock.exe')).VersionInfo.FileVersion -ne '1.1.0.0') { throw 'Incorrect application version' }
if ((Get-Item (Join-Path $release 'QuotaDock-1.1.0-Setup.exe')).VersionInfo.FileVersion -ne '1.1.0.0') { throw 'Incorrect installer version' }
$base = Join-Path $env:TEMP 'opencode'
if (!(Test-Path $base)) { $base = $env:TEMP }
$sandbox = Join-Path $base ('quotadock-install-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $sandbox 'Programs') -Force | Out-Null
try {
    Set-Content -LiteralPath (Join-Path $sandbox 'QuotaDock.InstallTest') -Value 'Test-only directory' -Encoding ASCII
    foreach ($name in @('QuotaDock.exe', 'Uninstall.ps1')) {
        $stream = $assembly.GetManifestResourceStream($name)
        $output = [IO.File]::Create((Join-Path $sandbox $name))
        try { $stream.CopyTo($output) } finally { $stream.Dispose(); $output.Dispose() }
    }
    if ((Get-FileHash (Join-Path $sandbox 'QuotaDock.exe')).Hash -ne (Get-FileHash (Join-Path $release 'QuotaDock.exe')).Hash) { throw 'Embedded executable differs from release' }
    Set-Content -LiteralPath (Join-Path $sandbox 'keep.txt') -Value 'Unrelated data' -Encoding ASCII
    foreach ($name in @('QuotaDock.lnk', 'Desinstalar QuotaDock.lnk')) { Set-Content -LiteralPath (Join-Path $sandbox "Programs\$name") -Value 'test' }
    & powershell.exe -NoProfile -File (Join-Path $sandbox 'Uninstall.ps1') -Quiet -TestRoot $sandbox
    if ($LASTEXITCODE -ne 0) { throw 'Sandbox uninstall failed' }
    if ((Test-Path (Join-Path $sandbox 'QuotaDock.exe')) -or (Test-Path (Join-Path $sandbox 'Uninstall.ps1')) -or !(Test-Path (Join-Path $sandbox 'keep.txt'))) { throw 'Uninstall removed wrong files' }
    Write-Host 'OK: installer resources, versions, embedded executable hash and sandbox uninstallation.'
} finally {
    # Only this run's unique, sentinel-marked test folder is removed.
    if (Test-Path -LiteralPath (Join-Path $sandbox 'QuotaDock.InstallTest')) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
}
