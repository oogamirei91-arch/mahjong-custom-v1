Add-Type -AssemblyName System.Drawing

$width = 2048
$height = 2048
$cols = 9
$rows = 5
$cellW = [int]($width / $cols)
$cellH = [int]($height / $rows)

# Create Bitmap & Graphics
$bmp = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Premium Color Palette
$colIvory = [System.Drawing.Color]::FromArgb(255, 252, 250, 242)
$colGoldBorder = [System.Drawing.Color]::FromArgb(255, 212, 175, 85)
$colGoldAccent = [System.Drawing.Color]::FromArgb(255, 230, 200, 115)
$colNavy = [System.Drawing.Color]::FromArgb(255, 16, 48, 140)
$colRuby = [System.Drawing.Color]::FromArgb(255, 218, 24, 32)
$colJade = [System.Drawing.Color]::FromArgb(255, 12, 136, 58)
$colCharcoal = [System.Drawing.Color]::FromArgb(255, 32, 32, 36)
$colWhite = [System.Drawing.Color]::White

$ivoryBrush = New-Object System.Drawing.SolidBrush($colIvory)
$goldPen = New-Object System.Drawing.Pen($colGoldBorder, 4)
$goldAccentPen = New-Object System.Drawing.Pen($colGoldAccent, 2)
$navyBrush = New-Object System.Drawing.SolidBrush($colNavy)
$rubyBrush = New-Object System.Drawing.SolidBrush($colRuby)
$jadeBrush = New-Object System.Drawing.SolidBrush($colJade)
$charcoalBrush = New-Object System.Drawing.SolidBrush($colCharcoal)
$whiteBrush = New-Object System.Drawing.SolidBrush($colWhite)

# Fonts
$badgeFont = New-Object System.Drawing.Font("Arial", 18, [System.Drawing.FontStyle]::Bold)
$kanjiFont = New-Object System.Drawing.Font("SimSun", 70, [System.Drawing.FontStyle]::Bold)
$wanFont   = New-Object System.Drawing.Font("SimSun", 66, [System.Drawing.FontStyle]::Bold)
$honorFont = New-Object System.Drawing.Font("SimSun", 92, [System.Drawing.FontStyle]::Bold)
$bonusFont = New-Object System.Drawing.Font("SimSun", 52, [System.Drawing.FontStyle]::Bold)

# String formats
$sfCenter = New-Object System.Drawing.StringFormat
$sfCenter.Alignment = [System.Drawing.StringAlignment]::Center
$sfCenter.LineAlignment = [System.Drawing.StringAlignment]::Center

$sfLeft = New-Object System.Drawing.StringFormat
$sfLeft.Alignment = [System.Drawing.StringAlignment]::Near
$sfLeft.LineAlignment = [System.Drawing.StringAlignment]::Near

# Fill Base
$g.FillRectangle($ivoryBrush, 0, 0, $width, $height)

# Unicode characters
$kanjiWanDigits = @(
    [char]0x4E00, # 一
    [char]0x4E8C, # 二
    [char]0x4E09, # 三
    [char]0x56DB, # 四
    [char]0x4E94, # 五
    [char]0x516D, # 六
    [char]0x4E03, # 七
    [char]0x516B, # 八
    [char]0x4E5D  # 九
)
$charWan = [char]0x842C # 萬

$honorChars = @(
    [char]0x6771, # 東 East
    [char]0x5357, # 南 South
    [char]0x897F, # 西 West
    [char]0x5317, # 北 North
    [char]0x4E2D, # 中 Red Dragon
    [char]0x767C, # 發 Green Dragon
    [char]0x767D  # 白 White Dragon
)
$honorBadges = @("E", "S", "W", "N", "C", "F", "P")

$flowerChars = @(
    [char]0x6885, # 梅 Plum
    [char]0x862D, # 蘭 Orchid
    [char]0x83CA, # 菊 Chrysanthemum
    [char]0x7AF9  # 竹 Bamboo
)

$seasonChars = @(
    [char]0x6625, # 春 Spring
    [char]0x590F, # 夏 Summer
    [char]0x79CB, # 秋 Autumn
    [char]0x51AC  # 冬 Winter
)

# Draw Cells
for ($r = 0; $r -lt $rows; $r++) {
    for ($c = 0; $c -lt $cols; $c++) {
        $startX = $c * $cellW
        $startY = $r * $cellH
        $cx = $startX + [int]($cellW / 2)
        $cy = $startY + [int]($cellH / 2)

        # 1. Bevel Border (Gold Double Rim)
        $margin = 6
        $rectW = $cellW - ($margin * 2)
        $rectH = $cellH - ($margin * 2)
        $g.DrawRectangle($goldPen, $startX + $margin, $startY + $margin, $rectW, $rectH)
        $g.DrawRectangle($goldAccentPen, $startX + $margin + 4, $startY + $margin + 4, $rectW - 8, $rectH - 8)

        # 2. Draw Content by Row
        if ($r -eq 0) {
            # Row 0: Characters / Wan (1 - 9)
            $val = $c + 1
            $g.DrawString("${val}W", $badgeFont, $rubyBrush, ($startX + 14), ($startY + 12), $sfLeft)

            $digitChar = [string]$kanjiWanDigits[$c]
            $g.DrawString($digitChar, $kanjiFont, $navyBrush, $cx, ($cy - 72), $sfCenter)
            $g.DrawString([string]$charWan, $wanFont, $rubyBrush, $cx, ($cy + 74), $sfCenter)
        }
        elseif ($r -eq 1) {
            # Row 1: Bamboo / Sou (1 - 9)
            $val = $c + 1
            $g.DrawString("${val}B", $badgeFont, $jadeBrush, ($startX + 14), ($startY + 12), $sfLeft)

            if ($val -eq 1) {
                # 1 Sou: Stylized Peacock / Sparrow
                $g.FillEllipse($jadeBrush, ($cx - 42), ($cy - 15), 84, 84)
                $g.FillEllipse($jadeBrush, ($cx - 25), ($cy - 68), 50, 50)
                $g.FillEllipse($rubyBrush, ($cx + 12), ($cy - 78), 24, 24)
                $g.FillEllipse($whiteBrush, ($cx + 4), ($cy - 65), 10, 10)
                $g.FillEllipse($navyBrush, ($cx + 6), ($cy - 63), 5, 5)
                # Wing feathers
                $g.FillEllipse($rubyBrush, ($cx - 52), ($cy - 12), 34, 34)
                $g.FillEllipse($navyBrush, ($cx + 20), ($cy - 12), 34, 34)
                $g.FillEllipse($whiteBrush, ($cx - 15), ($cy + 5), 30, 30)
                $g.FillEllipse($jadeBrush, ($cx - 10), ($cy + 10), 20, 20)
            }
            else {
                # Bamboo Sticks
                $stickW = 16
                $stickH = 68
                $rowsCount = 1
                if ($val -gt 6) { $rowsCount = 3 }
                elseif ($val -gt 3) { $rowsCount = 2 }

                $colsCount = [int][Math]::Ceiling($val / $rowsCount)
                $drawn = 0
                for ($sr = 0; $sr -lt $rowsCount; $sr++) {
                    $inThisRow = [Math]::Min($colsCount, ($val - $drawn))
                    
                    $yOffset = 0
                    if ($rowsCount -eq 2) {
                        if ($sr -eq 0) { $yOffset = -60 } else { $yOffset = 60 }
                    }
                    elseif ($rowsCount -eq 3) {
                        if ($sr -eq 0) { $yOffset = -75 }
                        elseif ($sr -eq 1) { $yOffset = 0 }
                        else { $yOffset = 75 }
                    }
                    $yPos = $cy + $yOffset

                    for ($sc = 0; $sc -lt $inThisRow; $sc++) {
                        $xPos = $cx + ($sc - ($inThisRow - 1) * 0.5) * 44
                        $bBrush = $jadeBrush
                        if ($val -eq 7 -and $sr -eq 0) { $bBrush = $rubyBrush }
                        elseif ($val -eq 8 -and $sr -eq 0) { $bBrush = $navyBrush }

                        $g.FillRectangle($bBrush, ($xPos - $stickW/2), ($yPos - $stickH/2), $stickW, $stickH)
                        $g.FillEllipse($bBrush, ($xPos - 12), ($yPos - 12), 24, 24)
                        $g.FillRectangle($whiteBrush, ($xPos - 2), ($yPos - $stickH/2 + 8), 4, ($stickH - 16))
                    }
                    $drawn += $inThisRow
                }
            }
        }
        elseif ($r -eq 2) {
            # Row 2: Dots / Pin (1 - 9)
            $val = $c + 1
            $g.DrawString("${val}D", $badgeFont, $navyBrush, ($startX + 14), ($startY + 12), $sfLeft)

            if ($val -eq 1) {
                # 1 Pin: Large Dragon Pearl / Wheel
                $g.FillEllipse($rubyBrush, ($cx - 68), ($cy - 68), 136, 136)
                $pRingJade = New-Object System.Drawing.Pen($colJade, 12)
                $pRingGold = New-Object System.Drawing.Pen($colGoldBorder, 6)
                $g.DrawEllipse($pRingJade, ($cx - 78), ($cy - 78), 156, 156)
                $g.DrawEllipse($pRingGold, ($cx - 88), ($cy - 88), 176, 176)
                $g.FillEllipse($whiteBrush, ($cx - 24), ($cy - 24), 48, 48)
                $g.FillEllipse($rubyBrush, ($cx - 14), ($cy - 14), 28, 28)
                $pRingJade.Dispose()
                $pRingGold.Dispose()
            }
            else {
                $pinR = 26
                $pinPositions = switch ($val) {
                    2 { @(@(0, -60, $jadeBrush), @(0, 60, $navyBrush)) }
                    3 { @(@(-44, -64, $navyBrush), @(0, 0, $rubyBrush), @(44, 64, $jadeBrush)) }
                    4 { @(@(-42, -56, $navyBrush), @(42, -56, $jadeBrush), @(-42, 56, $jadeBrush), @(42, 56, $navyBrush)) }
                    5 { @(@(-46, -62, $navyBrush), @(46, -62, $jadeBrush), @(0, 0, $rubyBrush), @(-46, 62, $jadeBrush), @(46, 62, $navyBrush)) }
                    6 { @(@(-42, -68, $jadeBrush), @(42, -68, $rubyBrush), @(-42, 0, $jadeBrush), @(42, 0, $rubyBrush), @(-42, 68, $jadeBrush), @(42, 68, $rubyBrush)) }
                    7 { @(@(-42, -78, $jadeBrush), @(0, -50, $jadeBrush), @(42, -22, $jadeBrush), @(-40, 34, $rubyBrush), @(40, 34, $rubyBrush), @(-40, 84, $rubyBrush), @(40, 84, $rubyBrush)) }
                    8 { @(@(-42, -78, $navyBrush), @(42, -78, $navyBrush), @(-42, -26, $navyBrush), @(42, -26, $navyBrush), @(-42, 26, $navyBrush), @(42, 26, $navyBrush), @(-42, 78, $navyBrush), @(42, 78, $navyBrush)) }
                    9 { @(@(-44, -68, $jadeBrush), @(0, -68, $jadeBrush), @(44, -68, $jadeBrush), @(-44, 0, $rubyBrush), @(0, 0, $rubyBrush), @(44, 0, $rubyBrush), @(-44, 68, $navyBrush), @(0, 68, $navyBrush), @(44, 68, $navyBrush)) }
                }
                foreach ($p in $pinPositions) {
                    $px = $cx + $p[0]
                    $py = $cy + $p[1]
                    $pBrush = $p[2]
                    $g.FillEllipse($pBrush, ($px - $pinR), ($py - $pinR), ($pinR*2), ($pinR*2))
                    $g.DrawEllipse($goldPen, ($px - $pinR - 2), ($py - $pinR - 2), (($pinR+2)*2), (($pinR+2)*2))
                    $g.FillEllipse($whiteBrush, ($px - 8), ($py - 8), 16, 16)
                    $g.FillEllipse($pBrush, ($px - 4), ($py - 4), 8, 8)
                }
            }
        }
        elseif ($r -eq 3) {
            # Row 3: Honors (East, South, West, North, Red Dragon, Green Dragon, White Dragon)
            if ($c -lt 7) {
                $badge = $honorBadges[$c]
                $bCol = $navyBrush
                if ($c -eq 4) { $bCol = $rubyBrush }
                elseif ($c -eq 5) { $bCol = $jadeBrush }

                $g.DrawString($badge, $badgeFont, $bCol, ($startX + 14), ($startY + 12), $sfLeft)

                if ($c -eq 4) {
                    # Red Dragon (中)
                    $g.DrawString([string]$honorChars[4], $honorFont, $rubyBrush, $cx, $cy, $sfCenter)
                }
                elseif ($c -eq 5) {
                    # Green Dragon (發)
                    $g.DrawString([string]$honorChars[5], $honorFont, $jadeBrush, $cx, $cy, $sfCenter)
                }
                elseif ($c -eq 6) {
                    # White Dragon (白 - Ornamental Classical Frame)
                    $pWhiteFrame = New-Object System.Drawing.Pen($colNavy, 12)
                    $g.DrawRectangle($pWhiteFrame, ($cx - 52), ($cy - 75), 104, 150)
                    $g.DrawRectangle($goldPen, ($cx - 40), ($cy - 63), 80, 126)
                    $pWhiteFrame.Dispose()
                }
                else {
                    # Winds (東, 南, 西, 北)
                    $g.DrawString([string]$honorChars[$c], $honorFont, $navyBrush, $cx, $cy, $sfCenter)
                }
            }
        }
        elseif ($r -eq 4) {
            # Row 4: Bonus (Flowers 1-4 & Seasons 1-4)
            if ($c -lt 4) {
                # Flower (梅, 蘭, 菊, 竹)
                $g.DrawString("F$($c+1)", $badgeFont, $rubyBrush, ($startX + 14), ($startY + 12), $sfLeft)
                $g.FillEllipse($rubyBrush, ($cx - 54), ($cy - 54), 108, 108)
                $g.DrawEllipse($goldPen, ($cx - 64), ($cy - 64), 128, 128)
                $g.DrawString([string]$flowerChars[$c], $bonusFont, $whiteBrush, $cx, $cy, $sfCenter)
            }
            elseif ($c -lt 8) {
                # Season (春, 夏, 秋, 冬)
                $sIdx = $c - 4
                $g.DrawString("S$($sIdx+1)", $badgeFont, $navyBrush, ($startX + 14), ($startY + 12), $sfLeft)
                $g.FillEllipse($navyBrush, ($cx - 54), ($cy - 54), 108, 108)
                $g.DrawEllipse($goldPen, ($cx - 64), ($cy - 64), 128, 128)
                $g.DrawString([string]$seasonChars[$sIdx], $bonusFont, $whiteBrush, $cx, $cy, $sfCenter)
            }
        }
    }
}

# Ensure destination folders exist
$resDir = "d:\Project SS\Mahjong\client\Assets\Resources"
$texDir = "d:\Project SS\Mahjong\client\Assets\Textures"
$spriteDir = "d:\Project SS\Mahjong\client\Assets\Textures\Sprites"

if (!(Test-Path $resDir)) { New-Item -ItemType Directory -Path $resDir -Force }
if (!(Test-Path $texDir)) { New-Item -ItemType Directory -Path $texDir -Force }
if (!(Test-Path $spriteDir)) { New-Item -ItemType Directory -Path $spriteDir -Force }

# 1. Save Full Atlas (2048x2048) to Resources and Textures
$resOut = Join-Path $resDir "CustomMahjongAtlas.png"
$texOut = Join-Path $texDir "Mahjong_Tiles_3D_Atlas.png"

$bmp.Save($resOut, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Save($texOut, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Saved 2048x2048 Atlas to: $resOut and $texOut"

# 2. Generate Jade Back Texture (512x512)
$jadeBmp = New-Object System.Drawing.Bitmap(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$jg = [System.Drawing.Graphics]::FromImage($jadeBmp)
$jg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

$jadeBackBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 10, 88, 48))
$jadePatternPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 18, 120, 68), 3)
$jadeGoldPen = New-Object System.Drawing.Pen($colGoldBorder, 6)
$jadeGoldAccent = New-Object System.Drawing.Pen($colGoldAccent, 2)

$jg.FillRectangle($jadeBackBrush, 0, 0, 512, 512)

# Diagonal diamond pattern for marble/jade luxury look
for ($i = -512; $i -lt 1024; $i += 48) {
    $jg.DrawLine($jadePatternPen, $i, 0, ($i + 512), 512)
    $jg.DrawLine($jadePatternPen, ($i + 512), 0, $i, 512)
}

# Gold border frames
$jg.DrawRectangle($jadeGoldPen, 16, 16, 480, 480)
$jg.DrawRectangle($jadeGoldAccent, 28, 28, 456, 456)
$jg.DrawEllipse($jadeGoldAccent, 156, 156, 200, 200)

$jadePath = Join-Path $texDir "Mahjong_Tile_JadeBack.png"
$jadeBmp.Save($jadePath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Saved Jade Back Texture to: $jadePath"

$jg.Dispose()
$jadeBmp.Dispose()

# 3. Export Individual Tile Sprites for UI / 2D use
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
    @("Flower_Plum", "Flower_Orchid", "Flower_Chrysanthemum", "Flower_Bamboo", "Season_Spring", "Season_Summer", "Season_Autumn", "Season_Winter", "Blank_Bonus_1")
)

for ($r = 0; $r -lt $rows; $r++) {
    for ($c = 0; $c -lt $cols; $c++) {
        $tName = $tileNames[$r][$c]
        if ($tName -match "Blank_") { continue }
        
        $srcRect = New-Object System.Drawing.Rectangle(($c * $cellW), ($r * $cellH), $cellW, $cellH)
        $singleBmp = New-Object System.Drawing.Bitmap($cellW, $cellH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $sg = [System.Drawing.Graphics]::FromImage($singleBmp)
        $sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $sg.DrawImage($bmp, 0, 0, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
        
        $singlePath = Join-Path $spriteDir "$tName.png"
        $singleBmp.Save($singlePath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        $sg.Dispose()
        $singleBmp.Dispose()
    }
}
Write-Output "Exported 42 Individual Tile Sprites to: $spriteDir"

# Cleanup
$goldPen.Dispose()
$goldAccentPen.Dispose()
$ivoryBrush.Dispose()
$navyBrush.Dispose()
$rubyBrush.Dispose()
$jadeBrush.Dispose()
$charcoalBrush.Dispose()
$whiteBrush.Dispose()
$badgeFont.Dispose()
$kanjiFont.Dispose()
$wanFont.Dispose()
$honorFont.Dispose()
$bonusFont.Dispose()

$g.Dispose()
$bmp.Dispose()

Write-Output "SUCCESS: All Mahjong 3D Sprites and Textures generated and saved to Assets!"
