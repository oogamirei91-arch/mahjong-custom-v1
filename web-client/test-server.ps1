$urls = @(
    "http://localhost:3000/",
    "http://localhost:3000/css/style.css",
    "http://localhost:3000/js/config.js",
    "http://localhost:3000/js/audio.js",
    "http://localhost:3000/js/tileAtlas.js",
    "http://localhost:3000/js/visualizer3d.js",
    "http://localhost:3000/js/gameLogic.js",
    "http://localhost:3000/js/aiManager.js",
    "http://localhost:3000/js/networkManager.js",
    "http://localhost:3000/js/main.js",
    "http://localhost:3000/manifest.json",
    "http://localhost:3000/assets/table_felt.png",
    "http://localhost:3000/assets/mahjong_atlas.png",
    "http://localhost:3000/icons/icon-192.png",
    "http://localhost:3000/icons/icon-512.png"
)

foreach ($url in $urls) {
    try {
        $res = Invoke-WebRequest -Uri $url -UseBasicParsing
        Write-Output "$url -> $($res.StatusCode)"
    } catch {
        Write-Output "$url -> ERROR: $_"
    }
}
