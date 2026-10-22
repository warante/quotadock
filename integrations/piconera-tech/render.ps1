$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$panelPath = Join-Path $root 'docs\images\provider-selector.png'
if (!(Test-Path -LiteralPath $panelPath)) { throw 'Build the synthetic UI preview before rendering the portfolio kit.' }
function Ink([string]$hex) { return [Drawing.ColorTranslator]::FromHtml($hex) }
function Rounded($g, $x, $y, $w, $h, $radius, $color) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $d = [single](2 * [Math]::Min($radius, [Math]::Min($w, $h) / 2))
    try {
        $path.AddArc([single]$x, [single]$y, $d, $d, 180, 90)
        $path.AddArc([single]($x+$w-$d), [single]$y, $d, $d, 270, 90)
        $path.AddArc([single]($x+$w-$d), [single]($y+$h-$d), $d, $d, 0, 90)
        $path.AddArc([single]$x, [single]($y+$h-$d), $d, $d, 90, 90)
        $path.CloseFigure()
        $brush = New-Object Drawing.SolidBrush((Ink $color))
        try { $g.FillPath($brush, $path) } finally { $brush.Dispose() }
    } finally { $path.Dispose() }
}
function Words($g, $text, $x, $y, $size, $color, $bold = $false) {
    $style = if ($bold) { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }
    $font = New-Object Drawing.Font('Segoe UI', [single]$size, $style, [Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object Drawing.SolidBrush((Ink $color))
    try { $g.DrawString($text, $font, $brush, [single]$x, [single]$y) } finally { $font.Dispose(); $brush.Dispose() }
}
function Banner([string]$language, [int]$width, [int]$height, [string]$path) {
    $image = New-Object Drawing.Bitmap($width, $height)
    $g = [Drawing.Graphics]::FromImage($image)
    $panel = [Drawing.Image]::FromFile($panelPath)
    try {
        $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $g.Clear((Ink '#12171f'))
        $g.ScaleTransform([single]($width / 1600), [single]($width / 1600))
        Rounded $g 1050 -125 530 530 265 '#1c2630'
        Rounded $g 1260 180 410 410 205 '#29292d'
        Rounded $g 121 93 9 70 4 '#c47a52'
        Words $g 'QuotaDock' 154 82 99 '#fffaf4' $true
        $headline = if ($language -eq 'es') { "Tus límites de IA,`na un vistazo." } else { "Your AI limits,`nat a glance." }
        $support = if ($language -eq 'es') { 'Un icono. Dos barras.' } else { 'One icon. Two bars.' }
        Words $g $headline 121 273 59 '#fffaf4' $true
        Words $g $support 126 447 30 '#b6c0d0'
        Rounded $g 124 522 156 45 22 '#2d2929'
        Words $g 'WINDOWS' 146 531 19 '#eda68a' $true
        Rounded $g 302 522 230 45 22 '#202a2d'
        Words $g 'OPEN SOURCE / MIT' 320 532 17 '#9bd4bf' $true
        Rounded $g 835 286 635 260 24 '#0b0f16'
        $g.DrawImage($panel, [single]850, [single]300, [single]605, [single](605 * $panel.Height / $panel.Width))
        Rounded $g 751 652 763 84 20 '#252c36'
        Rounded $g 1225 665 57 58 12 '#353e4d'
        Rounded $g 1237 677 3 33 1.5 '#eda68a'
        Rounded $g 1245 680 27 7 3 '#404b5b'
        Rounded $g 1245 680 4 7 2 '#ff7373'
        Rounded $g 1245 700 27 7 3 '#404b5b'
        Rounded $g 1245 700 16 7 3 '#66d7ac'
        Words $g '12:34' 1370 673 26 '#edf0f5'
        Words $g '^' 1170 681 23 '#a7b1c2'
        $demo = if ($language -eq 'es') { 'Vista de demostración · sin datos personales' } else { 'Demonstration preview · no personal data' }
        Words $g $demo 925 811 18 '#7e8da4'
        $image.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    } finally { $panel.Dispose(); $g.Dispose(); $image.Dispose() }
}
$banners = Join-Path $PSScriptRoot 'assets\apps\banners'
$social = Join-Path $PSScriptRoot 'social'
New-Item -ItemType Directory -Force -Path $banners, $social | Out-Null
Banner 'es' 1600 900 (Join-Path $banners 'quotadock.png')
Banner 'en' 1600 900 (Join-Path $banners 'quotadock-en.png')
Banner 'es' 1200 630 (Join-Path $social 'quotadock-og.png')
$icon = New-Object Drawing.Bitmap(512, 512)
$g = [Drawing.Graphics]::FromImage($icon)
try {
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([Drawing.Color]::Transparent)
    Rounded $g 0 0 512 512 112 '#1b1f27'
    $pen = New-Object Drawing.Pen((Ink '#c47a52'), 30)
    try { $g.DrawEllipse($pen, [single]96, [single]90, [single]296, [single]296) } finally { $pen.Dispose() }
    $pen = New-Object Drawing.Pen((Ink '#c47a52'), 34)
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round; $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    try { $g.DrawLine($pen, 346, 350, 413, 417) } finally { $pen.Dispose() }
    Rounded $g 150 196 188 26 13 '#3b4351'; Rounded $g 150 196 147 26 13 '#66d7ac'
    Rounded $g 150 254 188 26 13 '#3b4351'; Rounded $g 150 254 103 26 13 '#eda68a'
    $icon.Save((Join-Path $PSScriptRoot 'assets\apps\quotadock.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally { $g.Dispose(); $icon.Dispose() }
Write-Host 'OK: icono PNG, banners ES/EN y recurso social, con datos ficticios y gráficos propios.'
