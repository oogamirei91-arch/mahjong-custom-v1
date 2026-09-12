@echo off
title Mahjong VIP 3D Launcher
echo ===================================================
echo  🀄 MAHJONG VIP 3D - CLASSIC 144 TILES
echo ===================================================
echo Starting local web server on port 3000...
start /b powershell -ExecutionPolicy Bypass -File "%~dp0serve.ps1"
timeout /t 2 /nobreak >nul
echo Opening game in your default browser...
start http://localhost:3000/
echo.
echo Game is running at http://localhost:3000/
echo You can close this window to stop the server.
pause
