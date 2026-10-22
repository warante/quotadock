$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$card = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'project-card.html'))
$translations = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'i18n.json')) | ConvertFrom-Json
$keys = @([regex]::Matches($card, 'data-i18n(?:-[a-z]+)?="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
foreach ($language in @('es', 'en')) {
    foreach ($key in $keys) {
        if ($translations.$language.PSObject.Properties.Name -notcontains $key -or [string]::IsNullOrWhiteSpace($translations.$language.$key)) { throw "Missing $language translation: $key" }
    }
    $banner = ($translations.$language.'quotadock.banner' -split '\?')[0]
    if (!(Test-Path -LiteralPath (Join-Path $PSScriptRoot $banner))) { throw "Missing $language banner" }
}
if ($card -notmatch 'data-group="herramientas"' -or $card -notmatch 'class="card project"' -or $card -notmatch 'data-i18n-src="quotadock.banner"' -or $card -notmatch 'rel="noopener noreferrer"') { throw 'Card does not match the portfolio contract.' }
foreach ($entry in @(@('assets/apps/quotadock.png',512,512), @('assets/apps/banners/quotadock.png',1600,900), @('assets/apps/banners/quotadock-en.png',1600,900), @('social/quotadock-og.png',1200,630))) {
    $image = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot $entry[0]))
    try { if ($image.Width -ne $entry[1] -or $image.Height -ne $entry[2]) { throw "Wrong image dimensions: $($entry[0])" } } finally { $image.Dispose() }
}
[xml]$svg = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'assets/apps/quotadock.svg'))
if ($svg.OuterXml -match '(?i)<script|<foreignObject|href=') { throw 'Unexpected external or active SVG content.' }
Write-Host 'OK: portfolio card, ES/EN keys, banner paths, image dimensions and standalone SVG.'
