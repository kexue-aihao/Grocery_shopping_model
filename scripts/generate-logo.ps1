Add-Type -AssemblyName System.Drawing
function New-LogoPng($path, [int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $rect = New-Object System.Drawing.Rectangle 0,0,$size,$size
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(22,163,74)), ([System.Drawing.Color]::FromArgb(15,118,110)), 45
    $radius = [int]($size * 0.22)
    $pathBg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $pathBg.AddArc(0,0,$d,$d,180,90); $pathBg.AddArc($size-$d-1,0,$d,$d,270,90); $pathBg.AddArc($size-$d-1,$size-$d-1,$d,$d,0,90); $pathBg.AddArc(0,$size-$d-1,$d,$d,90,90); $pathBg.CloseFigure()
    $g.FillPath($bg, $pathBg)
    $plateSize = [int]($size * 0.53); $plateX = [int](($size-$plateSize)/2); $plateY = [int]($size*0.28)
    $plate = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Rectangle $plateX,$plateY,$plateSize,$plateSize), ([System.Drawing.Color]::White), ([System.Drawing.Color]::FromArgb(220,252,231)), 90
    $g.FillEllipse($plate, $plateX, $plateY, $plateSize, $plateSize)
    $penArc = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(190,187,247,208)), ([Math]::Max(6, $size*0.045))
    $penArc.StartCap = [System.Drawing.Drawing2D.LineCap]::Round; $penArc.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($penArc, [int]($size*.26), [int]($size*.18), [int]($size*.48), [int]($size*.20), 200, 140)
    $font = New-Object System.Drawing.Font 'Arial', ([int]($size*.105)), ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $sf = New-Object System.Drawing.StringFormat; $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
    $g.DrawString('BMI', $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF 0,([float]($size*.13)),$size,([float]($size*.12))), $sf)
    $green = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(34,197,94))
    $light = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(134,239,172))
    $leaf1 = New-Object System.Drawing.Drawing2D.GraphicsPath
    $leaf1.AddBezier([int]($size*.38),[int]($size*.61),[int]($size*.44),[int]($size*.43),[int]($size*.58),[int]($size*.40),[int]($size*.71),[int]($size*.36))
    $leaf1.AddBezier([int]($size*.71),[int]($size*.36),[int]($size*.68),[int]($size*.53),[int]($size*.54),[int]($size*.61),[int]($size*.38),[int]($size*.61))
    $g.FillPath($green,$leaf1)
    $leaf2 = New-Object System.Drawing.Drawing2D.GraphicsPath
    $leaf2.AddBezier([int]($size*.35),[int]($size*.42),[int]($size*.49),[int]($size*.41),[int]($size*.54),[int]($size*.52),[int]($size*.56),[int]($size*.60))
    $leaf2.AddBezier([int]($size*.56),[int]($size*.60),[int]($size*.42),[int]($size*.60),[int]($size*.34),[int]($size*.52),[int]($size*.35),[int]($size*.42))
    $g.FillPath($light,$leaf2)
    $penDark = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(190,15,23,42)), ([Math]::Max(8, $size*.05)); $penDark.StartCap='Round'; $penDark.EndCap='Round'
    $g.DrawLine($penDark, [int]($size*.36), [int]($size*.70), [int]($size*.64), [int]($size*.70))
    $penWhite = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([Math]::Max(4, $size*.023)); $penWhite.StartCap='Round'; $penWhite.EndCap='Round'
    $g.DrawLine($penWhite, [int]($size*.37), [int]($size*.70), [int]($size*.63), [int]($size*.70))
    $penOrange = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(249,115,22)), ([Math]::Max(8, $size*.047)); $penOrange.StartCap='Round'; $penOrange.EndCap='Round'; $penOrange.LineJoin='Round'
    $pts = @((New-Object System.Drawing.Point ([int]($size*.63),[int]($size*.64))), (New-Object System.Drawing.Point ([int]($size*.71),[int]($size*.72))), (New-Object System.Drawing.Point ([int]($size*.80),[int]($size*.57))))
    $g.DrawLines($penOrange, $pts)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}
function New-IcoFromPng($pngPath, $icoPath) {
    [byte[]]$png = [System.IO.File]::ReadAllBytes($pngPath)
    $fs = [System.IO.File]::Open($icoPath, [System.IO.FileMode]::Create)
    $bw = New-Object System.IO.BinaryWriter $fs
    $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]1)
    $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32); $bw.Write([UInt32]$png.Length); $bw.Write([UInt32]22)
    $bw.Write($png); $bw.Close(); $fs.Close()
}
New-LogoPng 'assets/logo-1024.png' 1024
New-LogoPng 'assets/logo-512.png' 512
New-LogoPng 'assets/logo-192.png' 192
New-LogoPng 'desktop/GroceryBmi.App/Assets/logo.png' 256
New-IcoFromPng 'desktop/GroceryBmi.App/Assets/logo.png' 'desktop/GroceryBmi.App/Assets/logo.ico'
Copy-Item 'assets/logo-512.png' 'android_flutter/grocery_bmi_app/assets/logo.png' -Force
