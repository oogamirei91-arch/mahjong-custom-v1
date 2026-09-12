# ==============================================================================
# SCRIPT OTOMATIS: UPLOAD & MIGRASI DATABASE MAHJONG CUSTOM v1.0 KE SUPABASE
# ==============================================================================

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  MENGUPLOAD SKEMA DATABASE KE SUPABASE POSTGRESQL..." -ForegroundColor Yellow
Write-Host "==================================================================" -ForegroundColor Cyan

$GoBinary = "C:\Program Files\Go\bin\go.exe"
if (-not (Test-Path $GoBinary)) {
    $GoBinary = (Get-Command go -ErrorAction SilentlyContinue).Source
}

if (-not $GoBinary) {
    Write-Host "Go binary tidak ditemukan di sistem." -ForegroundColor Red
    exit 1
}

Push-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)

try {
    & $GoBinary run ./cmd/migrate/main.go
} finally {
    Pop-Location
}
