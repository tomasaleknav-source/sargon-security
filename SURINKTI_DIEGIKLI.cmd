@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 goto missing_dotnet
if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" goto build
where makensis >nul 2>&1
if errorlevel 1 goto missing_nsis
:build
echo Building SARGON installer. Please wait...
call build-windows.cmd
if errorlevel 1 goto failed
echo.
echo Installer created in the parent folder:
echo %~dp0..\SARGON_Security_PC_v0.49_Diegimas.exe
echo This is an unsigned test build. Test on Windows before relying on it.
pause
exit /b 0
:missing_dotnet
echo ERROR: .NET SDK not found. Install a compatible .NET SDK and .NET Framework 4.8 development tools.
pause
exit /b 1
:missing_nsis
echo ERROR: NSIS not found. Install NSIS and retry.
pause
exit /b 1
:failed
echo ERROR: Build failed. Copy or photograph the error messages above.
pause
exit /b 1
