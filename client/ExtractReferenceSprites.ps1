Add-Type -AssemblyName System.Drawing

$refPath = "d:\Project SS\Mahjong\client\Assets\Textures\Mahjong_Reference_Spritesheet.jpg"
if (!(Test-Path $refPath)) {
    Write-Error "Reference spritesheet not found at $refPath"
    exit 1
}

$refBmp = [System.Drawing.Bitmap]::FromFile($refPath)

$atlasW = 2048
$atlasH = 2048
$cols = 9
$rows = 5
$cellW = [int]($atlasW / $cols) # 227
$cellH = [int]($atlasH / $rows) # 409

$atlasBmp = New-Object System.Drawing.Bitmap($atlasW, $atlasH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$ag = [System.Drawing.Graphics]::FromImage($atlasBmp)
$ag.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$ag.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$ag.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

# Background fill with Ivory
$ivoryBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 252, 250, 242))
$goldPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 212, 175, 85), 3)
$ag.FillRectangle($ivoryBrush, 0, 0, $atlasW, $atlasH)

# Exact bounding boxes for every single tile in the reference sheet
# 1. Characters / Wan (Row 0 in Atlas Grid)
$charBoxes = @(
    @{ X=7;   Y=470; W=90; H=148 },
    @{ X=110; Y=470; W=90; H=148 },
    @{ X=216; Y=470; W=90; H=148 },
    @{ X=351; Y=470; W=90; H=148 },
    @{ X=468; Y=470; W=90; H=148 },
    @{ X=591; Y=470; W=90; H=148 },
    @{ X=719; Y=470; W=90; H=148 },
    @{ X=825; Y=470; W=90; H=148 },
    @{ X=928; Y=470; W=90; H=148 }
)

# 2. Bamboo / Sou (Row 1 in Atlas Grid)
$bambooBoxes = @(
    @{ X=7;   Y=5; W=90; H=125 },
    @{ X=110; Y=5; W=90; H=125 },
    @{ X=216; Y=5; W=90; H=125 },
    @{ X=351; Y=5; W=90; H=125 },
    @{ X=468; Y=5; W=90; H=125 },
    @{ X=591; Y=5; W=90; H=125 },
    @{ X=719; Y=5; W=90; H=125 },
    @{ X=825; Y=5; W=90; H=125 },
    @{ X=928; Y=5; W=90; H=125 }
)

# 3. Dots / Pin (Row 2 in Atlas Grid)
$dotBoxes = @(
    @{ X=7;   Y=290; W=90; H=135 },
    @{ X=110; Y=290; W=90; H=135 },
    @{ X=216; Y=290; W=90; H=135 },
    @{ X=351; Y=290; W=90; H=135 },
    @{ X=468; Y=290; W=90; H=135 },
    @{ X=591; Y=290; W=90; H=135 },
    @{ X=719; Y=290; W=90; H=135 },
    @{ X=825; Y=290; W=90; H=135 },
    @{ X=928; Y=290; W=90; H=135 }
)

# 4. Honors (Row 3 in Atlas Grid: East, South, West, North, Red Dragon, Green Dragon, White Dragon)
$honorBoxes = @(
    @{ X=6;   Y=682; W=92; H=138 }, # East
    @{ X=110; Y=682; W=93; H=138 }, # South
    @{ X=226; Y=682; W=95; H=138 }, # West
    @{ X=354; Y=682; W=95; H=138 }, # North
    @{ X=588; Y=682; W=94; H=138 }, # Red Dragon (中)
    @{ X=714; Y=682; W=93; H=138 }, # Green Dragon (發)
    @{ X=805; Y=682; W=95; H=138 }  # White Dragon (白)
)

# 5. Flowers & Seasons (Row 4 in Atlas Grid: 4 Flowers + 4 Seasons)
$flowerSeasonBoxes = @(
    @{ X=5;   Y=881; W=108; H=138 }, # Plum (梅)
    @{ X=132; Y=881; W=108; H=138 }, # Orchid (蘭)
    @{ X=262; Y=881; W=108; H=138 }, # Bamboo (竹)
    @{ X=392; Y=881; W=108; H=138 }, # Chrysanthemum (菊)
    @{ X=520; Y=881; W=108; H=138 }, # Spring (春)
    @{ X=646; Y=881; W=108; H=138 }, # Summer (夏)
    @{ X=772; Y=881; W=108; H=138 }, # Autumn (秋)
    @{ X=904; Y=881; W=108; H=138 }  # Winter (冬)
)

function DrawTileToAtlasCell($srcBox, $gridRow, $gridCol) {
    $destX = $gridCol * $cellW
    $destY = $gridRow * $cellH
    
    # Fill ivory tile background
    $ag.FillRectangle($ivoryBrush, $destX, $destY, $cellW, $cellH)
    
    # Draw Bevel border
    $margin = 4
    $ag.DrawRectangle($goldPen, ($destX + $margin), ($destY + $margin), ($cellW - $margin*2), ($cellH - $margin*2))
    
    # Draw reference tile image fitted within cell
    $srcRect = New-Object System.Drawing.Rectangle($srcBox.X, $srcBox.Y, $srcBox.W, $srcBox.H)
    $padX = 8
    $padY = 8
    $destRect = New-Object System.Drawing.Rectangle(($destX + $padX), ($destY + $padY), ($cellW - $padX*2), ($cellH - $padY*2))
    $ag.DrawImage($refBmp, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
}

# Row 0: Characters
for ($c = 0; $c -lt 9; $c++) {
    DrawTileToAtlasCell $charBoxes[$c] 0 $c
}

# Row 1: Bamboo
for ($c = 0; $c -lt 9; $c++) {
    DrawTileToAtlasCell $bambooBoxes[$c] 1 $c
}

# Row 2: Dots
for ($c = 0; $c -lt 9; $c++) {
    DrawTileToAtlasCell $dotBoxes[$c] 2 $c
}

# Row 3: Honors
for ($c = 0; $c -lt $honorBoxes.Count; $c++) {
    DrawTileToAtlasCell $honorBoxes[$c] 3 $c
}

# Row 4: Flowers & Seasons
for ($c = 0; $c -lt $flowerSeasonBoxes.Count; $c++) {
    DrawTileToAtlasCell $flowerSeasonBoxes[$c] 4 $c
}

# Save output files
$resOut = "d:\Project SS\Mahjong\client\Assets\Resources\CustomMahjongAtlas.png"
$texOut = "d:\Project SS\Mahjong\client\Assets\Textures\Mahjong_Tiles_3D_Atlas.png"
$spriteDir = "d:\Project SS\Mahjong\client\Assets\Textures\Sprites"

$atlasBmp.Save($resOut, [System.Drawing.Imaging.ImageFormat]::Png)
$atlasBmp.Save($texOut, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Saved 2048x2048 Atlas from Reference Image to: $resOut and $texOut"

# Export 42 Individual Sprites
$tileNames = @(
    # Row 0: Characters (Wan)
    @("1_Wan", "2_Wan", "3_Wan", "4_Wan", "5_Wan", "6_Wan", "7_Wan", "8_Wan", "9_Wan"),
    # Row 1: Bamboo (Sou)
    @("1_Bamboo", "2_Bamboo", "3_Bamboo", "4_Bamboo", "5_Bamboo", "6_Bamboo", "7_Bamboo", "8_Bamboo", "9_Bamboo"),
    # Row 2: Dots (Pin)
    @("1_Dot", "2_Dot", "3_Dot", "4_Dot", "5_Dot", "6_Dot", "7_Dot", "8_Dot", "9_Dot"),
    # Row 3: Honors
    @("East_Wind", "South_Wind", "West_Wind", "North_Wind", "Red_Dragon", "Green_Dragon", "White_Dragon", "Blank_Honor_1", "Blank_Honor_2"),
    # Row 4: Bonus
    @("Flower_Plum", "Flower_Orchid", "Flower_Bamboo", "Flower_Chrysanthemum", "Season_Spring", "Season_Summer", "Season_Autumn", "Season_Winter", "Blank_Bonus_1")
)

for ($r = 0; $r -lt $rows; $r++) {
    for ($c = 0; $c -lt $cols; $c++) {
        $tName = $tileNames[$r][$c]
        if ($tName -match "Blank_") { continue }

        $srcRect = New-Object System.Drawing.Rectangle(($c * $cellW), ($r * $cellH), $cellW, $cellH)
        $singleBmp = New-Object System.Drawing.Bitmap($cellW, $cellH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $sg = [System.Drawing.Graphics]::FromImage($singleBmp)
        $sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $sg.DrawImage($atlasBmp, 0, 0, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

        $singlePath = Join-Path $spriteDir "$tName.png"
        $singleBmp.Save($singlePath, [System.Drawing.Imaging.ImageFormat]::Png)

        $sg.Dispose()
        $singleBmp.Dispose()
    }
}
Write-Output "Successfully updated 42 individual sprites matching reference style!"

$ag.Dispose()
$atlasBmp.Dispose()
$refBmp.Dispose()
$ivoryBrush.Dispose()
$goldPen.Dispose()

Write-Output "DONE!"
