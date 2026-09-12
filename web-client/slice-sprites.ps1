Add-Type -AssemblyName System.Drawing

$atlasPath = "d:\Project SS\Mahjong\web-client\assets\mahjong_atlas.png"
$webOutputDir = "d:\Project SS\Mahjong\web-client\assets\sprites"
$unityOutputDir = "d:\Project SS\Mahjong\client\Assets\Textures\Sprites"

New-Item -ItemType Directory -Force -Path $webOutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $unityOutputDir | Out-Null

$srcBmp = [System.Drawing.Bitmap]::FromFile($atlasPath)
$totalCols = 9
$totalRows = 5

$cellW = $srcBmp.Width / $totalCols
$cellH = $srcBmp.Height / $totalRows

$tiles = @(
    # Row 0: Characters / Wan (1 - 9)
    @{ row = 0; col = 0; name = "Wan_1" },
    @{ row = 0; col = 1; name = "Wan_2" },
    @{ row = 0; col = 2; name = "Wan_3" },
    @{ row = 0; col = 3; name = "Wan_4" },
    @{ row = 0; col = 4; name = "Wan_5" },
    @{ row = 0; col = 5; name = "Wan_6" },
    @{ row = 0; col = 6; name = "Wan_7" },
    @{ row = 0; col = 7; name = "Wan_8" },
    @{ row = 0; col = 8; name = "Wan_9" },

    # Row 1: Bamboo / Sou (1 - 9)
    @{ row = 1; col = 0; name = "Bamboo_1" },
    @{ row = 1; col = 1; name = "Bamboo_2" },
    @{ row = 1; col = 2; name = "Bamboo_3" },
    @{ row = 1; col = 3; name = "Bamboo_4" },
    @{ row = 1; col = 4; name = "Bamboo_5" },
    @{ row = 1; col = 5; name = "Bamboo_6" },
    @{ row = 1; col = 6; name = "Bamboo_7" },
    @{ row = 1; col = 7; name = "Bamboo_8" },
    @{ row = 1; col = 8; name = "Bamboo_9" },

    # Row 2: Dots / Pin (1 - 9)
    @{ row = 2; col = 0; name = "Dot_1" },
    @{ row = 2; col = 1; name = "Dot_2" },
    @{ row = 2; col = 2; name = "Dot_3" },
    @{ row = 2; col = 3; name = "Dot_4" },
    @{ row = 2; col = 4; name = "Dot_5" },
    @{ row = 2; col = 5; name = "Dot_6" },
    @{ row = 2; col = 6; name = "Dot_7" },
    @{ row = 2; col = 7; name = "Dot_8" },
    @{ row = 2; col = 8; name = "Dot_9" },

    # Row 3: Winds & Dragons
    @{ row = 3; col = 0; name = "Wind_East" },
    @{ row = 3; col = 1; name = "Wind_South" },
    @{ row = 3; col = 2; name = "Wind_West" },
    @{ row = 3; col = 3; name = "Wind_North" },
    @{ row = 3; col = 4; name = "Dragon_Red" },
    @{ row = 3; col = 5; name = "Dragon_Green" },
    @{ row = 3; col = 6; name = "Dragon_White" },

    # Row 4: Flowers & Seasons
    @{ row = 4; col = 0; name = "Flower_Plum" },
    @{ row = 4; col = 1; name = "Flower_Orchid" },
    @{ row = 4; col = 2; name = "Flower_Bamboo" },
    @{ row = 4; col = 3; name = "Flower_Chrysanthemum" },
    @{ row = 4; col = 4; name = "Season_Spring" },
    @{ row = 4; col = 5; name = "Season_Summer" },
    @{ row = 4; col = 6; name = "Season_Autumn" },
    @{ row = 4; col = 7; name = "Season_Winter" }
)

Write-Output "Memotong $($tiles.Count) ubin dari atlas mahjong dengan pembersihan border..."

$padLeft = 3.5
$padRight = 3.5

foreach ($t in $tiles) {
    # Penyesuaian padding vertikal agar bebas dari bar hitam
    $padTop = if ($t.row -eq 0) { 16.0 } else { 7.0 }
    $padBottom = if ($t.row -eq 0) { 4.0 } else { 6.0 }

    $srcX = [float]($t.col * $cellW + $padLeft)
    $srcY = [float]($t.row * $cellH + $padTop)
    $srcW = [float]($cellW - ($padLeft + $padRight))
    $srcH = [float]($cellH - ($padTop + $padBottom))

    $destW = 128
    $destH = 180

    $targetBmp = New-Object System.Drawing.Bitmap($destW, $destH)
    $g = [System.Drawing.Graphics]::FromImage($targetBmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

    $destRect = New-Object System.Drawing.RectangleF(0.0, 0.0, [float]$destW, [float]$destH)
    $srcRect = New-Object System.Drawing.RectangleF($srcX, $srcY, $srcW, $srcH)

    $g.DrawImage($srcBmp, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $webFile = Join-Path $webOutputDir "$($t.name).png"
    $unityFile = Join-Path $unityOutputDir "$($t.name).png"

    $targetBmp.Save($webFile, [System.Drawing.Imaging.ImageFormat]::Png)
    $targetBmp.Save($unityFile, [System.Drawing.Imaging.ImageFormat]::Png)

    $g.Dispose()
    $targetBmp.Dispose()
}

# Generate Tile_Back.png (Jade Green Back)
$backW = 128
$backH = 180
$backBmp = New-Object System.Drawing.Bitmap($backW, $backH)
$bg = [System.Drawing.Graphics]::FromImage($backBmp)
$bg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

$jadeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 11, 124, 59))
$bg.FillRectangle($jadeBrush, 0, 0, $backW, $backH)

$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 218, 178, 80), [float]3)
$bg.DrawRectangle($borderPen, [float]6, [float]6, [float]($backW - 12), [float]($backH - 12))

$backWeb = Join-Path $webOutputDir "Tile_Back.png"
$backUnity = Join-Path $unityOutputDir "Tile_Back.png"
$backBmp.Save($backWeb, [System.Drawing.Imaging.ImageFormat]::Png)
$backBmp.Save($backUnity, [System.Drawing.Imaging.ImageFormat]::Png)

$bg.Dispose()
$backBmp.Dispose()
$srcBmp.Dispose()

Write-Output "Berhasil mengekstrak $($tiles.Count + 1) file sprite ubin mahjong ultra-bersih!"
