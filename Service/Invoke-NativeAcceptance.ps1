#Requires -Version 5.1
#Requires -RunAsAdministrator
[CmdletBinding()]param([switch]$DetectionControl,[switch]$DatabaseReadiness,[switch]$InstallDisposableService)
$ErrorActionPreference='Stop'
$reports=Join-Path $env:TEMP ('SARGON-acceptance-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $reports | Out-Null
$badTokens=$null;$parseErrors=$null
foreach($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1'){$null=[Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$badTokens,[ref]$parseErrors);if($parseErrors.Count){throw ('PowerShell parse errors: '+$file.Name)}}
$argsForDriver=@();if($DatabaseReadiness -and !$InstallDisposableService){$argsForDriver+='--readiness'};if($DetectionControl -and !$InstallDisposableService){$argsForDriver+='--detection'}
& (Join-Path $PSScriptRoot 'SARGON.NativeAcceptance.exe') @argsForDriver | Tee-Object -FilePath (Join-Path $reports 'native-file-actions.txt')
if($LASTEXITCODE -ne 0){throw 'Native file-action/detection acceptance not confirmed; see report'}
if($InstallDisposableService){
 if(Get-Service SARGONProtection -ErrorAction SilentlyContinue){throw 'Existing service found. Disposable test refuses to replace it.'}
 if(Test-Path -LiteralPath (Join-Path $env:ProgramData 'SARGONService')){throw 'Existing service data found. Disposable test refuses to reuse it.'}
 $fixture=Join-Path $env:PUBLIC ('SARGON-neutral-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $fixture | Out-Null;[IO.File]::WriteAllText((Join-Path $fixture 'neutral.txt'),'SARGON safe neutral service acceptance file.')
 try{
  & (Join-Path $PSScriptRoot 'Manage-Service.ps1') -Action Install -Package $PSScriptRoot -ScanRoot @($fixture) -AllowUnsignedBeta
  $statusPath=Join-Path $env:ProgramData 'SARGONService\Public\status.json';$confirmed=$false;$deadline=[DateTime]::UtcNow.AddMinutes(20)
  while([DateTime]::UtcNow -lt $deadline){Start-Sleep -Seconds 2;$s=Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json;$s|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $reports 'service-status.json');if($s.DirectoryChecks -ge 1){$confirmed=$s.Unverified -eq 0;break};if($s.State -eq 'ERROR_PROTECTION_UNCONFIRMED'){break}}
  if(!$confirmed){throw 'Neutral service cycle not confirmed within deadline'}
  if($DetectionControl){& (Join-Path $PSScriptRoot 'SARGON.NativeAcceptance.exe') --detection | Tee-Object -FilePath (Join-Path $reports 'service-detection-control.txt');if($LASTEXITCODE -ne 0){throw 'Local engine EICAR control not confirmed'}}
  $previousRun=$s.RunId;Stop-Service SARGONProtection;(Get-Service SARGONProtection).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30));$plan=Join-Path $env:ProgramData 'SARGONService\Private\service-plan.json';$before=Get-Content -LiteralPath $plan -Raw;$restartAt=[DateTime]::UtcNow.Ticks;Start-Service SARGONProtection;$resumed=$false;$restartDeadline=[DateTime]::UtcNow.AddSeconds(60);while([DateTime]::UtcNow -lt $restartDeadline){Start-Sleep -Milliseconds 500;$again=Get-Content -LiteralPath $statusPath -Raw|ConvertFrom-Json;if($again.RunId -ne $previousRun -and $again.At -ge $restartAt -and $again.DirectoryChecks -ge 1){$resumed=$true;break}};if(!$resumed){throw 'Fresh resumed service heartbeat not confirmed'};if((Get-Content -LiteralPath $plan -Raw) -ne $before){throw 'Completed plan changed unexpectedly after service restart'}
  Write-Output 'PASS: neutral cycle and persisted completed plan after service restart. Reboot, false positives, real malware efficacy and update rollback still need separate Windows tests.'
 }finally{if(Get-Service SARGONProtection -ErrorAction SilentlyContinue){& (Join-Path $PSScriptRoot 'Manage-Service.ps1') -Action Uninstall};if(Test-Path -LiteralPath $fixture){Remove-Item -LiteralPath $fixture -Recurse -Force}}
}
Write-Output ('Reports: '+$reports)
