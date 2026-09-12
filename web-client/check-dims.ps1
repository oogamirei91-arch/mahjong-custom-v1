Add-Type -AssemblyName System.Drawing
$atlas = [System.Drawing.Image]::FromFile('d:\Project SS\Mahjong\web-client\assets\mahjong_atlas.png')
$felt = [System.Drawing.Image]::FromFile('d:\Project SS\Mahjong\web-client\assets\table_felt.png')
Write-Output "Atlas Size: $($atlas.Width) x $($atlas.Height)"
Write-Output "Felt Size: $($felt.Width) x $($felt.Height)"
$atlas.Dispose()
$felt.Dispose()
