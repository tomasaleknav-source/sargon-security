#Requires -Version 5.1
[CmdletBinding()]param([switch]$SkipBuild,[string]$Output=(Join-Path $PSScriptRoot 'package'))
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
if(!$SkipBuild){foreach($project in @('TelefonoSargasPC.csproj','Service/SargonService.csproj','Service/NativeAcceptance.csproj','Service/CaseReview.csproj','Service/Setup.csproj')){& dotnet build (Join-Path $root $project) -c Release;if($LASTEXITCODE -ne 0){throw 'Build failed'}}}
New-Item -ItemType Directory -Path $Output -Force|Out-Null
$main=Join-Path $root 'bin/Release/net48/TelefonoSargasPC.exe';$serviceBin=Join-Path $PSScriptRoot 'bin/Release/net48'
Copy-Item -LiteralPath $main -Destination $Output;Copy-Item -LiteralPath (Join-Path $root 'app.config') -Destination (Join-Path $Output 'TelefonoSargasPC.exe.config');Copy-Item -LiteralPath (Join-Path $root 'initial.hdb') -Destination $Output
foreach($name in @('SARGON.Service.exe','SARGON.NativeAcceptance.exe','SARGON.CaseReview.exe','SARGON.Setup.exe')){Copy-Item -LiteralPath (Join-Path $serviceBin $name) -Destination $Output}
foreach($name in @('Manage-Service.ps1','Provision-ServiceEngine.ps1','Sign-ServiceRelease.ps1','Sign-WindowsRelease.ps1','Invoke-NativeAcceptance.ps1','README_LT.md')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $Output}
$names=@('TelefonoSargasPC.exe','TelefonoSargasPC.exe.config','initial.hdb','SARGON.Service.exe','SARGON.NativeAcceptance.exe','SARGON.CaseReview.exe','SARGON.Setup.exe','Manage-Service.ps1','Provision-ServiceEngine.ps1','Sign-ServiceRelease.ps1','Sign-WindowsRelease.ps1','Invoke-NativeAcceptance.ps1','README_LT.md')
$files=@($names|ForEach-Object {@{Name=$_;Hash=(Get-FileHash -LiteralPath (Join-Path $Output $_) -Algorithm SHA256).Hash.ToLowerInvariant()}})
[IO.File]::WriteAllText((Join-Path $Output 'release.json'),(@{Version=49;Files=$files;Signature=''}|ConvertTo-Json -Depth 5 -Compress),(New-Object Text.UTF8Encoding($false)))
Write-Output 'Unsigned test package created. For production use, sign the manifest and establish the publisher SHA256 pin independently. No test certificate is shipped.'
