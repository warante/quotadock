$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Static design explorations only. All quotas and the clock below are fictional.
$script:fonts = @{}
function Font([int]$size, [bool]$bold = $false) {
    $key = "$size-$bold"
    if (!$script:fonts.ContainsKey($key)) {
        $style = if ($bold) { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }
        $script:fonts[$key] = New-Object Drawing.Font('Segoe UI', $size, $style, [Drawing.GraphicsUnit]::Pixel)
    }
    return $script:fonts[$key]
}
function Color([string]$hex) { return [Drawing.ColorTranslator]::FromHtml($hex) }
function Box($x, $y, $w, $h, $fill, $radius = 0) {
    $brush = New-Object Drawing.SolidBrush((Color $fill))
    try {
        if ($radius -le 0) { $script:g.FillRectangle($brush, [single]$x, [single]$y, [single]$w, [single]$h); return }
        $path = New-Object Drawing.Drawing2D.GraphicsPath
        try {
            $d = 2 * $radius
            $path.AddArc([single]$x, [single]$y, [single]$d, [single]$d, 180, 90)
            $path.AddArc([single]($x+$w-$d), [single]$y, [single]$d, [single]$d, 270, 90)
            $path.AddArc([single]($x+$w-$d), [single]($y+$h-$d), [single]$d, [single]$d, 0, 90)
            $path.AddArc([single]$x, [single]($y+$h-$d), [single]$d, [single]$d, 90, 90)
            $path.CloseFigure(); $script:g.FillPath($brush, $path)
        } finally { $path.Dispose() }
    } finally { $brush.Dispose() }
}
function Text($text, $x, $y, $size = 18, $color = '#EDF0F5', $bold = $false) {
    $brush = New-Object Drawing.SolidBrush((Color $color))
    try { $script:g.DrawString($text, (Font $size $bold), $brush, [single]$x, [single]$y) } finally { $brush.Dispose() }
}
function Line($x1, $y1, $x2, $y2, $color = '#3B424E', $width = 1) {
    $pen = New-Object Drawing.Pen((Color $color), [single]$width)
    try { $script:g.DrawLine($pen, [single]$x1, [single]$y1, [single]$x2, [single]$y2) } finally { $pen.Dispose() }
}
function Meter($x, $y, $width, $percent, $height = 10) {
    $fill = if ($percent -le 10) { '#FF7373' } elseif ($percent -le 25) { '#F2BD64' } else { '#66D7AC' }
    Box $x $y $width $height '#3B424E' ($height / 2)
    if ($percent -gt 0) { Box $x $y ([Math]::Max($height, $width * $percent / 100)) $height $fill ($height / 2) }
}
function Dot($x, $y, $fill) { Box $x $y 9 9 $fill 4.5 }
function Begin($number, $title, $description) {
    $script:bitmap = New-Object Drawing.Bitmap(960, 650)
    $script:g = [Drawing.Graphics]::FromImage($script:bitmap)
    $script:g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $script:g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $script:g.Clear((Color '#10141C'))
    Text "QUOTADOCK / CONCEPTO $number" 38 25 14 '#8C9AB0' $true
    Text $title 36 58 30 '#F3F5F9' $true
    Text $description 38 105 17 '#A7B1C2'
    Text 'Boceto de interfaz · datos ficticios · no modifica la aplicación' 38 555 14 '#69778E'
    Box 0 596 960 54 '#20242B'
    Line 0 596 960 596 '#353C47'
    # Generic taskbar context; no real desktop or account data.
    Line 710 626 716 620 '#C5CCD7' 2; Line 716 620 722 626 '#C5CCD7' 2
    $pen = New-Object Drawing.Pen((Color '#C5CCD7'), 1.8)
    try { $script:g.DrawArc($pen, 778, 613, 24, 18, 210, 120); $script:g.DrawArc($pen, 784, 619, 12, 9, 210, 120) } finally { $pen.Dispose() }
    Box 813 616 22 12 '#C5CCD7' 2; Box 815 618 16 8 '#20242B' 1; Box 836 619 3 6 '#C5CCD7' 1
    Text '12:34' 863 604 18 '#F1F3F7'; Text '22/10' 865 625 13 '#A4AFC1'
    # One highlighted system-tray slot, instead of one icon per provider.
    Box 732 603 38 39 '#343B47' 7
}
function TrayBars($top, $bottom, $accent = '#A3ADC1') {
    Box 740 612 2 23 $accent 1
    Meter 745 614 19 $top 5
    Meter 745 627 19 $bottom 5
}
function Panel($x, $y, $w, $h) {
    Box ($x + 2) ($y + 8) $w $h '#090C12' 15
    Box $x $y $w $h '#242A35' 15
}
function Finish($name) {
    $script:bitmap.Save((Join-Path $PSScriptRoot $name), [Drawing.Imaging.ImageFormat]::Png)
    $script:g.Dispose(); $script:bitmap.Dispose()
}

try {
    Begin 'A' 'Un icono con el saldo mínimo' 'Vista de seguridad: avisa del límite más cercano, sea del proveedor que sea.'
    Panel 385 169 535 365
    Text 'Todos tus proveedores' 412 189 21 '#F4F5F8' $true
    Text 'El icono resume el mínimo de cada ventana' 412 222 14 '#9FACC0'
    Line 412 253 892 253
    $providers = @(
        @('OpenAI', '#66D7AC', 81, 64),
        @('Claude', '#EDA68A', 7, 58),
        @('Kimi', '#6CAEFF', 46, 32)
    )
    for ($i=0; $i -lt 3; $i++) {
        $p = $providers[$i]; $y = 269 + 82*$i
        Dot 415 ($y+7) $p[1]; Text $p[0] 434 $y 18 '#EDF0F5' $true
        Text '5 h' 550 ($y+1) 14 '#A7B1C2'; Meter 599 ($y+7) 222 $p[2] 8; Text "$($p[2])%" 839 ($y-1) 17
        Text 'Semana' 550 ($y+28) 14 '#A7B1C2'; Meter 621 ($y+34) 200 $p[3] 8; Text "$($p[3])%" 839 ($y+26) 17
    }
    Text '5 h: Claude 7% · Semana: Kimi 32%' 38 204 19 '#F1F3F7' $true
    Text "Una sola pareja de barras.`nAl pulsar: detalle de todos.`n`nLas dos barras pueden representar`nproveedores distintos." 38 246 17 '#9FAEC4'
    TrayBars 7 32 '#A3ADC1'
    Finish '01-minimos.png'

    Begin 'B' 'Un icono con el proveedor elegido' 'Vista enfocada: conserva las dos barras actuales y permite cambiar de proveedor.'
    Panel 385 284 535 250
    Box 412 310 143 39 '#303845' 9; Text 'OpenAI' 449 318 17 '#A7B1C2'
    Box 565 310 143 39 '#5A413A' 9; Text 'Claude' 606 318 17 '#F5C2AD' $true
    Box 718 310 174 39 '#303845' 9; Text 'Kimi' 781 318 17 '#A7B1C2'
    Text '5 h' 414 388 18 '#D5DCE8'; Meter 515 400 285 7 12; Text '7%' 835 385 22 '#FF8D8D' $true
    Text 'Semana' 414 450 18 '#D5DCE8'; Meter 515 462 285 58 12; Text '58%' 823 447 22 '#EDF0F5' $true
    Text 'Ahora estás viendo Claude' 38 204 19 '#F1F3F7' $true
    Text "Un icono, dos límites, una cuenta.`nPulsar y elegir otra pestaña.`n`nEs la opción más parecida`na la interfaz actual." 38 246 17 '#9FAEC4'
    TrayBars 7 58 '#EDA68A'
    Finish '02-selector.png'

    Begin 'C' 'Icono fijo + resumen compacto' 'Vista limpia: el icono identifica la aplicación y el detalle aparece solo al pulsar.'
    Panel 385 234 535 300
    Text 'Tus límites' 413 255 22 '#EDF0F5' $true
    Text '5 h' 637 301 14 '#99A7BD'; Text 'Semana' 792 301 14 '#99A7BD'
    $providers = @(
        @('OpenAI', '#66D7AC', 81, 64),
        @('Claude', '#EDA68A', 7, 58),
        @('Kimi', '#6CAEFF', 46, 32)
    )
    for ($i=0; $i -lt 3; $i++) {
        $p = $providers[$i]; $y = 338 + 60*$i
        if ($i -eq 1) { Box 402 ($y-4) 500 54 '#312C34' 7 }
        Dot 416 ($y+12) $p[1]; Text $p[0] 437 ($y+3) 18 '#EDF0F5' $true
        $tone = if ($p[2] -le 10) { '#FF8D8D' } else { '#EDF0F5' }
        Text "$($p[2])%" 626 $y 22 $tone $true; Meter 596 ($y+32) 117 $p[2] 5
        Text "$($p[3])%" 793 $y 22 '#EDF0F5' $true; Meter 759 ($y+32) 117 $p[3] 5
    }
    Text 'La barra de tareas queda más limpia' 38 204 18 '#F1F3F7' $true
    Text "Un único icono fijo de QuotaDock.`nPunto rojo opcional si queda poca cuota.`n`nNo muestra porcentajes hasta pulsar,`npero permite comparar todos de un vistazo." 38 246 17 '#9FAEC4'
    Text 'Q' 740 606 27 '#E6ECF7' $true; Box 759 609 7 7 '#FF7373' 3.5
    Finish '03-panel.png'
} finally {
    foreach ($font in $script:fonts.Values) { $font.Dispose() }
}
Write-Host 'Tres bocetos PNG generados con datos ficticios.'
