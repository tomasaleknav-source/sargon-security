@echo off
setlocal
cd /d "%~dp0"

dotnet build TelefonoSargasPC.csproj -c Release
if errorlevel 1 exit /b 1
dotnet build Service\SargonService.csproj -c Release
if errorlevel 1 exit /b 1
dotnet build Service\Setup.csproj -c Release
if errorlevel 1 exit /b 1
dotnet build Service\CaseReview.csproj -c Release
if errorlevel 1 exit /b 1
dotnet build Service\NativeAcceptance.csproj -c Release
if errorlevel 1 exit /b 1

dotnet run --project tests\SecurityReadinessTests.csproj -c Release
if errorlevel 1 exit /b 1
dotnet run --project tests\RealtimeMonitorTests.csproj -c Release
if errorlevel 1 exit /b 1

echo.
echo Host logic tests passed. Run Service\Invoke-NativeAcceptance.ps1 as administrator on a Windows test PC.
echo This script does not validate Authenticode signing, service ACLs, reboot behavior, detection efficacy, or performance.
endlocal
