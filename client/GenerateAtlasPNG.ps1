Add-Type -AssemblyName System.Drawing

$width = 2048
$height = 2048
$cols = 9
$rows = 5
$cellW = [int]($width / $cols)
$cellH = [int]($height / $rows)

$bmp = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# Colors
$ivoryBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 251, 250, 244))
$goldPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 218, 184, 96), 4)
$goldBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 218, 184, 96))
$navyBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 16, 46, 148))
$redBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 220, 26, 32))
$greenBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 10, 138, 62))
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$blackBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 30, 34))

# Fonts
$badgeFont = New-Object System.Drawing.Font("Arial", 18, [System.Drawing.FontStyle]::Bold)
$kanjiFont = New-Object System.Drawing.Font("SimSun", 72, [System.Drawing.FontStyle]::Bold)
$wanFont   = New-Object System.Drawing.Font("SimSun", 68, [System.Drawing.FontStyle]::Bold)
$honorFont = New-Object System.Drawing.Font("SimSun", 100, [System.Drawing.FontStyle]::Bold)
$bonusFont = New-Object System.Drawing.Font("SimSun", 54, [System.Drawing.FontStyle]::Bold)

# String formats
$sfCenter = New-Object System.Drawing.StringFormat
$sfCenter.Alignment = [System.Drawing.StringAlignment]::Center
$sfCenter.LineAlignment = [System.Drawing.StringAlignment]::Center

$sfLeft = New-Object System.Drawing.StringFormat
$sfLeft.Alignment = [System.Drawing.StringAlignment]::Near
$sfLeft.LineAlignment = [System.Drawing.StringAlignment]::Near

# Fill Background
$g.FillRectangle($ivoryBrush, 0, 0, $width, $height)

# Draw Cells
for ($r = 0; $r -lt $rows; $r++) {
    for ($c = 0; $c -lt $cols; $c++) {
        $startX = $c * $cellW
        # Texture2D coordinates: row 0 is top of UV, which is top of image
        $startY = $r * $cellH
        $cx = $startX + [int]($cellW / 2)
        $cy = $startY + [int]($cellH / 2)

        # 1. Bevel Border
        $margin = 6
        $rectW = $cellW - ($margin * 2)
        $rectH = $cellH - ($margin * 2)
        $g.DrawRectangle($goldPen, $startX + $margin, $startY + $margin, $rectW, $rectH)

        # 2. Cell Content by Row
        if ($r -eq 0) {
            # Characters / Wan (1 - 9)
            $val = $c + 1
            $g.DrawString("${val}W", $badgeFont, $redBrush, ($startX + 14), ($startY + 12), $sfLeft)

            $kanjiDigits = @("一", "二", "三", "四", "五", "六", "七", "八", "九")
            $digitChar = $kanjiDigits[$c]
            $g.DrawString($digitChar, $kanjiFont, $navyBrush, $cx, ($cy - 75), $sfCenter)
            $g.DrawString("萬", $wanFont, $redBrush, $cx, ($cy + 75), $sfCenter)
        }
        elseif ($r -eq 1) {
            # Bamboo / Sou (1 - 9)
            $val = $c + 1
            $g.DrawString("${val}B", $badgeFont, $greenBrush, ($startX + 14), ($startY + 12), $sfLeft)

            if ($val -eq 1) {
                # 1 Sou: Peacock
                $g.FillEllipse($greenBrush, ($cx - 40), ($cy - 20), 80, 80)
                $g.FillEllipse($greenBrush, ($cx - 24), ($cy - 70), 48, 48)
                $g.FillEllipse($redBrush, ($cx + 10), ($cy - 80), 22, 22)
                $g.FillEllipse($whiteBrush, ($cx + 4), ($cy - 65), 10, 10)
                $g.FillEllipse($redBrush, ($cx - 50), ($cy - 10), 30, 30)
                $g.FillEllipse($navyBrush, ($cx + 20), ($cy - 10), 30, 30)
            }
            else {
                # Draw Sticks
                $stickW = 16
                $stickH = 65
                $rowsCount = if ($val -gt 6) { 3 } elseif ($val -gt 3) { 2 } else { 1 }
                $colsCount = [int][Math]::Ceiling($val / $rowsCount)
                $drawn = 0
                for ($sr = 0; $sr -lt $rowsCount; $sr++) {
                    $inThisRow = [Math]::Min($colsCount, ($val - $drawn))
                    $yPos = $cy + (if ($rowsCount -eq 1) { 0 } elseif ($sr -eq 0) { -58 } elseif ($rowsCount -eq 2) { 58 } elseif ($sr -eq 1) { 0 } else { 75 })
                    for ($sc = 0; $sc -lt $inThisRow; $sc++) {
                        $xPos = $cx + ($sc - ($inThisRow - 1) * 0.5) * 44
                        $brush = if ($val -eq 7 -and $sr -eq 0) { $redBrush } else { $greenBrush }
                        $g.FillRectangle($brush, ($xPos - $stickW/2), ($yPos - $stickH/2), $stickW, $stickH)
                        $g.FillEllipse($brush, ($xPos - 12), ($yPos - 12), 24, 24)
                        $g.FillRectangle($whiteBrush, ($xPos - 2), ($yPos - $stickH/2 + 8), 4, ($stickH - 16))
                    }
                    $drawn += $inThisRow
                }
            }
        }
        elseif ($r -eq 2) {
            # Dots / Pin (1 - 9)
            $val = $c + 1
            $g.DrawString("${val}D", $badgeFont, $navyBrush, ($startX + 14), ($startY + 12), $sfLeft)

            if ($val -eq 1) {
                # 1 Pin
                $g.FillEllipse($redBrush, ($cx - 68), ($cy - 68), 136, 136)
                $g.DrawEllipse(New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 10, 138, 62), 12), ($cx - 78), ($cy - 78), 156, 156)
                $g.DrawEllipse(New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 218, 184, 96), 6), ($cx - 88), ($cy - 88), 176, 176)
                $g.FillEllipse($whiteBrush, ($cx - 24), ($cy - 24), 48, 48)
                $g.FillEllipse($redBrush, ($cx - 14), ($cy - 14), 28, 28)
            }
            else {
                # Pins
                $pinR = 26
                $pinPositions = switch ($val) {
                    2 { @(@(0, -60, $greenBrush), @(0, 60, $navyBrush)) }
                    3 { @(@(-44, -64, $navyBrush), @(0, 0, $redBrush), @(44, 64, $greenBrush)) }
                    4 { @(@(-42, -56, $navyBrush), @(42, -56, $greenBrush), @(-42, 56, $greenBrush), @(42, 56, $navyBrush)) }
                    5 { @(@(-46, -62, $navyBrush), @(46, -62, $greenBrush), @(0, 0, $redBrush), @(-46, 62, $greenBrush), @(46, 62, $navyBrush)) }
                    6 { @(@(-42, -68, $greenBrush), @(42, -68, $redBrush), @(-42, 0, $greenBrush), @(42, 0, $redBrush), @(-42, 68, $greenBrush), @(42, 68, $redBrush)) }
                    7 { @(@(-42, -78, $greenBrush), @(0, -50, $greenBrush), @(42, -22, $greenBrush), @(-40, 34, $redBrush), @(40, 34, $redBrush), @(-40, 84, $redBrush), @(40, 84, $redBrush)) }
                    8 { @(@(-42, -78, $navyBrush), @(42, -78, $navyBrush), @(-42, -26, $navyBrush), @(42, -26, $navyBrush), @(-42, 26, $navyBrush), @(42, 26, $navyBrush), @(-42, 78, $navyBrush), @(42, 78, $navyBrush)) }
                    9 { @(@(-44, -68, $greenBrush), @(0, -68, $greenBrush), @(44, -68, $greenBrush), @(-44, 0, $redBrush), @(0, 0, $redBrush), @(44, 0, $redBrush), @(-44, 68, $navyBrush), @(0, 68, $navyBrush), @(44, 68, $navyBrush)) }
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
            # Honors: Winds (0..3) & Dragons (4..6)
            $honorNames = @("東", "南", "西", "北", "中", "發", "白")
            $honorBadges = @("E", "S", "W", "N", "C", "F", "P")
            if ($c -lt 7) {
                $badge = $honorBadges[$c]
                $bCol = if ($c -eq 4) { $redBrush } elseif ($c -eq 5) { $greenBrush } else { $navyBrush }
                $g.DrawString($badge, $badgeFont, $bCol, ($startX + 14), ($startY + 12), $sfLeft)

                if ($c -eq 4) {
                    # Red Dragon (中)
                    $g.DrawString("中", $honorFont, $redBrush, $cx, $cy, $sfCenter)
                }
                elseif ($c -eq 5) {
                    # Green Dragon (發)
                    $g.DrawString("發", $honorFont, $greenBrush, $cx, $cy, $sfCenter)
                }
                elseif ($c -eq 6) {
                    # White Dragon (白 - Blank Ornamental Frame)
                    $g.DrawRectangle(New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 16, 46, 148), 10), ($cx - 52), ($cy - 75), 104, 150)
                    $g.DrawRectangle($goldPen, ($cx - 40), ($cy - 63), 80, 126)
                }
                else {
                    # Winds (東, 南, 西, 北)
                    $g.DrawString($honorNames[$c], $honorFont, $navyBrush, $cx, $cy, $sfCenter)
                }
            }
        }
        elseif ($r -eq 4) {
            # Bonus: Flowers (0..3) & Seasons (4..7)
            $flowerNames = @("梅", "蘭", "菊", "竹")
            $seasonNames = @("春", "夏", "秋", "冬")
            if ($c -lt 4) {
                $g.DrawString("F$($c+1)", $badgeFont, $redBrush, ($startX + 14), ($startY + 12), $sfLeft)
                $g.FillEllipse($redBrush, ($cx - 52), ($cy - 52), 104, 104)
                $g.DrawEllipse($goldPen, ($cx - 62), ($cy - 62), 124, 124)
                $g.DrawString($flowerNames[$c], $bonusFont, $whiteBrush, $cx, $cy, $sfCenter)
            }
            elseif ($c -lt 8) {
                $sIdx = $c - 4
                $g.DrawString("S$($sIdx+1)", $badgeFont, $navyBrush, ($startX + 14), ($startY + 12), $sfLeft)
                $g.FillEllipse($navyBrush, ($cx - 52), ($cy - 52), 104, 104)
                $g.DrawEllipse($goldPen, ($cx - 62), ($cy - 62), 124, 124)
                $g.DrawString($seasonNames[$sIdx], $bonusFont, $whiteBrush, $cx, $cy, $sfCenter)
            }
        }
    }
}

# Save output files
$resOut = "d:\Project SS\Mahjong\client\Assets\Resources\CustomMahjongAtlas.png"
$texOut = "d:\Project SS\Mahjong\client\Assets\Textures\Mahjong_Tiles_3D_Atlas.png"

$bmp.Save($resOut, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Save($texOut, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()

Write-Output "Successfully generated 2048x2048 Ultra-HD Mahjong Spritesheet PNG files!"
