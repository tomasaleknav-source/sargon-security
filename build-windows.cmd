@echo off
setlocal
cd /d "%~dp0"
if "%SARGON_VERSION%"=="" set "SARGON_VERSION=0.50"
dotnet build TelefonoSargasPC.csproj -c Release
if errorlevel 1 exit /b 1
dotnet build Service\SargonService.csproj -c Release
if errorlevel 1 exit /b 1
if not exist build mkdir build
copy /y bin\Release\net48\TelefonoSargasPC.exe build\TelefonoSargasPC.exe
if errorlevel 1 exit /b 1
copy /y bin\Release\net48\TelefonoSargasPC.exe.config build\TelefonoSargasPC.exe.config
if errorlevel 1 exit /b 1
copy /y Service\bin\Release\net48\SARGON.Service.exe build\SARGON.Service.exe
if errorlevel 1 exit /b 1
dotnet build Service\NativeAcceptance.csproj -c Release
if errorlevel 1 exit /b 1
dotnet build Service\CaseReview.csproj -c Release
if errorlevel 1 exit /b 1
dotnet build Service\Setup.csproj -c Release
if errorlevel 1 exit /b 1
powershell -NoProfile -File Service\Build-ServicePackage.ps1 -SkipBuild -Output build\service-package
if errorlevel 1 exit /b 1
if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" (
  "%ProgramFiles(x86)%\NSIS\makensis.exe" /DAPP_VERSION=%SARGON_VERSION% installer.nsi
) else (
  makensis /DAPP_VERSION=%SARGON_VERSION% installer.nsi
)
if errorlevel 1 exit /b 1
