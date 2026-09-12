$urls = @(
    'http://localhost:3000/',
    'http://localhost:3000/index.html',
    'http://localhost:3000/css/style.css',
    'http://localhost:3000/js/main.js',
    'http://localhost:3000/js/networkManager.js',
    'http://localhost:3000/js/gameLogic.js',
    'http://localhost:3000/js/aiManager.js',
    'http://localhost:3000/js/visualizer3d.js',
    'http://localhost:3000/js/tileAtlas.js',
    'http://localhost:3000/js/config.js',
    'http://localhost:3000/assets/table_felt.png',
    'http://localhost:3000/assets/mahjong_atlas.png'
)

foreach ($u in $urls) {
    try {
        $r = Invoke-WebRequest -Uri $u -Method Head -UseBasicParsing
        Write-Host "$($r.StatusCode) OK -> $u"
    } catch {
        Write-Host "ERR -> $u : $_"
    }
}
