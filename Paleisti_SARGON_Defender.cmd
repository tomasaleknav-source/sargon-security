@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -STA -File "%~dp0SARGON_Defender_Valdymas.ps1"
if errorlevel 1 pause
