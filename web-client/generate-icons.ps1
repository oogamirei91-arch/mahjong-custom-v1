Add-Type -AssemblyName System.Drawing

function Create-MahjongIcon($size, $outputPath) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

    # Background
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 6, 21, 16))
    $g.FillRectangle($bgBrush, 0, 0, $size, $size)

    # Gold border
    $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 212, 175, 55), [float]($size * 0.04))
    $g.DrawRectangle($borderPen, [float]($size * 0.05), [float]($size * 0.05), [float]($size * 0.9), [float]($size * 0.9))

    # Inner Ivory Tile
    $tileBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 250, 247, 240))
    $g.FillRectangle($tileBrush, [float]($size * 0.22), [float]($size * 0.15), [float]($size * 0.56), [float]($size * 0.7))

    # Red Dragon '中'
    $font = New-Object System.Drawing.Font("Arial", [float]($size * 0.35), [System.Drawing.FontStyle]::Bold)
    $redBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 200, 20, 24))
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = New-Object System.Drawing.RectangleF([float]($size * 0.22), [float]($size * 0.15), [float]($size * 0.56), [float]($size * 0.7))
    $g.DrawString("中", $font, $redBrush, $rect, $sf)

    $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

Create-MahjongIcon 192 "d:\Project SS\Mahjong\web-client\icons\icon-192.png"
Create-MahjongIcon 512 "d:\Project SS\Mahjong\web-client\icons\icon-512.png"
Write-Output "Icons generated successfully!"
