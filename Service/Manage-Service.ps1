#Requires -Version 5.1
#Requires -RunAsAdministrator
[CmdletBinding()]param([ValidateSet('Install','Uninstall','Status','ResetPlan','Cases')][string]$Action='Status',[string]$Package=$PSScriptRoot,[string[]]$ScanRoot=@("$env:SystemDrive\"),[string]$PublisherSha256='',[switch]$AllowUnsignedBeta,[switch]$SkipEngineDownload)
$ErrorActionPreference='Stop'
if($env:OS -ne 'Windows_NT' -or ![Environment]::Is64BitOperatingSystem -or ![Environment]::Is64BitProcess){throw 'Use64-bit Windows PowerShell on Windows x64'}
if((Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full').Release -lt 528040){throw '.NET Framework4.8 required'}
$name='SARGONProtection';$program=Join-Path $env:ProgramFiles 'SARGONService';$data=Join-Path $env:ProgramData 'SARGONService';$private=Join-Path $data 'Private';$public=Join-Path $data 'Public'
function Assert-NoLink([string]$Path){$p=[IO.Path]::GetFullPath($Path);while($p){if(Test-Path -LiteralPath $p){if((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse point rejected'}};$p=[IO.Path]::GetDirectoryName($p)}}
function Protect([string]$Path,[string]$ServiceSid,[bool]$Modify,[bool]$ReadUsers=$false){Assert-NoLink $Path;if(!(Test-Path -LiteralPath $Path)){New-Item -ItemType Directory -Path $Path | Out-Null};$acl=New-Object Security.AccessControl.DirectorySecurity;$acl.SetAccessRuleProtection($true,$false);$admins=New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544');$system=New-Object Security.Principal.SecurityIdentifier('S-1-5-18');$acl.SetOwner($admins);foreach($sid in @($admins,$system)){$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid,'FullControl','ContainerInherit,ObjectInherit','None','Allow')))};if($ServiceSid){$rights=if($Modify){'Modify'}else{'ReadAndExecute'};$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier($ServiceSid)),$rights,'ContainerInherit,ObjectInherit','None','Allow')))};if($ReadUsers){$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier('S-1-5-11')),'ReadAndExecute','ContainerInherit,ObjectInherit','None','Allow')))};Set-Acl -LiteralPath $Path -AclObject $acl}
function Sc([string[]]$Arguments){& "$env:SystemRoot\System32\sc.exe" @Arguments | Write-Verbose;if($LASTEXITCODE -ne 0){throw 'Service control command failed'}}
function Stop-OwnService{$s=Get-Service $name -ErrorAction SilentlyContinue;if($s -and $s.Status -ne 'Stopped'){Stop-Service $name;$s.WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))}}
function Verify-Package([string]$Folder,[int]$Minimum){
 Assert-NoLink $Folder;$manifestPath=Join-Path $Folder 'release.json';Assert-NoLink $manifestPath;if((Get-Item -LiteralPath $manifestPath).Length -gt 65536){throw 'Manifest too large'};$m=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
 if($m.Version -lt $Minimum -or $m.Version -le 0 -or @($m.Files).Count -lt 1 -or @($m.Files).Count -gt 64){throw 'Release version or file count rejected'}
 $names=@{};$canonical="SARGON|$($m.Version)`n"
 $ordered=[string[]]@($m.Files.Name);[Array]::Sort($ordered,[StringComparer]::Ordinal)
 foreach($entryName in $ordered){$file=@($m.Files | Where-Object {$_.Name -ceq $entryName})[0]
  if($file.Name -cnotmatch '^[A-Za-z0-9_.-]{1,100}$' -or $file.Name -in @('.','..') -or $names.ContainsKey($file.Name.ToLowerInvariant()) -or $file.Hash -notmatch '^[a-fA-F0-9]{64}$'){throw 'Invalid release file'};$names[$file.Name.ToLowerInvariant()]=$true
  $path=Join-Path $Folder $file.Name;Assert-NoLink $path;if((Get-Item -LiteralPath $path).Length -gt 52428800){throw 'Payload file too large'};if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $file.Hash){throw "Release file digest mismatch: $($file.Name)"};$canonical+="$($file.Name)|$($file.Hash.ToLowerInvariant())`n"
 }
 foreach($required in @('SARGON.Service.exe','TelefonoSargasPC.exe','TelefonoSargasPC.exe.config','initial.hdb','Provision-ServiceEngine.ps1')){if(!$names.ContainsKey($required.ToLowerInvariant())){throw 'Required release payload missing'}}
 if($PublisherSha256){if($PublisherSha256 -notmatch '^[a-fA-F0-9]{64}$'){throw 'Invalid publisher pin'};$certPath=Join-Path $Folder 'publisher.cer';Assert-NoLink $certPath;if((Get-Item -LiteralPath $certPath).Length -gt 16384){throw 'Publisher certificate too large'};if((Get-FileHash -LiteralPath $certPath -Algorithm SHA256).Hash -ine $PublisherSha256){throw 'Publisher pin mismatch'};$cert=New-Object Security.Cryptography.X509Certificates.X509Certificate2($certPath);try{$rsa=[Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPublicKey($cert);try{if(!$rsa -or $rsa.ExportParameters($false).Modulus.Length -lt 256 -or !$rsa.VerifyData([Text.Encoding]::UTF8.GetBytes($canonical),[Convert]::FromBase64String($m.Signature),[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)){throw 'Release signature rejected'}}finally{if($rsa){$rsa.Dispose()}}}finally{$cert.Dispose()}}
 elseif(!$AllowUnsignedBeta){throw 'No trusted publisher pin. Unsigned test packages require explicit -AllowUnsignedBeta; never treat this as production trust.'}
 return $m
}
foreach($path in @($program,$data)){Assert-NoLink $path}
switch($Action){
 'Status' {Get-Service $name -ErrorAction SilentlyContinue;Get-Content -LiteralPath (Join-Path $public 'status.json') -ErrorAction SilentlyContinue;return}
 'Cases' {Get-ChildItem -LiteralPath (Join-Path $private 'cases') -Filter '*.json' -ErrorAction SilentlyContinue | ForEach-Object {Get-Content -LiteralPath $_.FullName};return}
 'Uninstall' {Stop-OwnService;Sc -Arguments @('delete',$name);Write-Output 'Service removed. Program files, reports, plans and user DPAPI quarantine are retained.';return}
 'ResetPlan' {Stop-OwnService;$plan=Join-Path $private 'service-plan.json';Assert-NoLink $plan;if(Test-Path -LiteralPath $plan){Move-Item -LiteralPath $plan -Destination ($plan+'.'+[Guid]::NewGuid().ToString('N')+'.bak')};Write-Output 'Plan archived. Start the service to create a new cycle.';return}
}
foreach($protectedPath in @($program,$data)){if(!(Test-Path -LiteralPath $protectedPath)){Protect $protectedPath '' $false}else{$owner=(Get-Acl -LiteralPath $protectedPath).GetOwner([Security.Principal.SecurityIdentifier]).Value;if($owner -notin @('S-1-5-32-544','S-1-5-18')){throw 'Existing service directory is not owned by Administrators or SYSTEM; refusing to trust it'}}}
$pinFile=Join-Path $program 'publisher-pin.txt';if(Test-Path -LiteralPath $pinFile){$oldPin=(Get-Content -LiteralPath $pinFile -Raw).Trim();if($oldPin -and $oldPin -ine $PublisherSha256){throw 'Existing publisher pin cannot be changed or removed by this update'}}
$floorFile=Join-Path $program 'release-floor.txt';$floor=0;if(Test-Path -LiteralPath $floorFile){$floor=[int](Get-Content -LiteralPath $floorFile -Raw)}
$m=Verify-Package $Package $floor
$roots=@($ScanRoot | ForEach-Object {$p=[IO.Path]::GetFullPath($_);if($p.StartsWith('\\') -or $p.Length -gt 512 -or !(Test-Path -LiteralPath $p -PathType Container)){throw 'Local scan root unavailable'};Assert-NoLink $p;$p} | Select-Object -Unique);if($roots.Count -lt 1 -or $roots.Count -gt 16){throw 'Configure1..16 local roots'}
$versions=Join-Path $program 'versions';New-Item -ItemType Directory -Path $versions -Force | Out-Null;$destination=Join-Path $versions ($m.Version.ToString()+'-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $destination | Out-Null
foreach($file in $m.Files){Copy-Item -LiteralPath (Join-Path $Package $file.Name) -Destination (Join-Path $destination $file.Name)};Copy-Item -LiteralPath (Join-Path $Package 'release.json') -Destination $destination;if($PublisherSha256){Copy-Item -LiteralPath (Join-Path $Package 'publisher.cer') -Destination $destination};$null=Verify-Package $destination $floor
$existing=Get-CimInstance Win32_Service -Filter "Name='$name'";$oldPath=if($existing){$existing.PathName}else{''};if($existing -and $existing.StartName -ine ('NT SERVICE\'+$name)){throw 'Existing service account is unexpected; refusing to replace it'}
Stop-OwnService
$backup=Join-Path $program ('backup-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $backup | Out-Null
foreach($n in @('service-plan.json','settings.json')){$source=Join-Path $private $n;if(Test-Path -LiteralPath $source){Assert-NoLink $source;Copy-Item -LiteralPath $source -Destination (Join-Path $backup $n)}}
$bin='"'+(Join-Path $destination 'SARGON.Service.exe')+'"'
if($existing){$result=Invoke-CimMethod -InputObject $existing -MethodName Change -Arguments @{PathName=$bin;StartMode='Automatic'};if($result.ReturnValue -ne 0){throw 'Service path update failed'}}else{$result=Invoke-CimMethod -ClassName Win32_Service -MethodName Create -Arguments @{Name=$name;DisplayName='SARGON Protection (experimental)';PathName=$bin;ServiceType=[uint32]16;ErrorControl=[uint32]1;StartMode='Automatic';DesktopInteract=$false;StartName=('NT SERVICE\'+$name);StartPassword=''};if($result.ReturnValue -ne 0){throw 'Service registration failed'}}
try {
 Sc -Arguments @('sidtype',$name,'unrestricted');$sid=([Security.Principal.NTAccount]::new('NT SERVICE\'+$name)).Translate([Security.Principal.SecurityIdentifier]).Value
 Protect $program $sid $false $true;Protect $data $sid $false $true;Protect $private $sid $true;Protect $public $sid $true $true;Protect (Join-Path $data "Databases") $sid $true $true
 $configPath=Join-Path $program 'service-config.json'
 if(!(Test-Path -LiteralPath $configPath)){[IO.File]::WriteAllText($configPath,(@{Roots=$roots;AutomaticUpdates=$true;DetectionControl=$false;PeriodHours=24}|ConvertTo-Json -Compress),(New-Object Text.UTF8Encoding($false)))}
 elseif($ScanRoot -and $existing){Write-Output 'Existing protected scan roots/config retained. To change roots, stop service, edit service-config.json as administrator, archive/reset the plan, then restart.'}
 if(!(Test-Path -LiteralPath (Join-Path $program 'engine')) -and !$SkipEngineDownload){& (Join-Path $destination 'Provision-ServiceEngine.ps1')}
 $configAcl=New-Object Security.AccessControl.FileSecurity;$configAcl.SetAccessRuleProtection($true,$false);$configAcl.SetOwner((New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')));foreach($identity in @('S-1-5-32-544','S-1-5-18')){$configAcl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier($identity)),'FullControl','Allow')))};$configAcl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier($sid)),'Read','Allow')));Set-Acl -LiteralPath $configPath -AclObject $configAcl
 $legacyDb=Join-Path $private 'clamav\database';$sharedDb=Join-Path $data 'Databases';if((Test-Path -LiteralPath $legacyDb) -and !(Get-ChildItem -LiteralPath $sharedDb -File)){Get-ChildItem -LiteralPath $legacyDb -File | Where-Object {$_.Extension -in @('.cvd','.cld')} | ForEach-Object {Assert-NoLink $_.FullName;Copy-Item -LiteralPath $_.FullName -Destination $sharedDb}}
 Sc -Arguments @('config',$name,'start=','delayed-auto');Sc -Arguments @('failure',$name,'reset=','86400','actions=','restart/60000/restart/300000/none/0')
 $begin=[DateTime]::UtcNow.Ticks;Start-Service $name;$ok=$false;$deadline=[DateTime]::UtcNow.AddSeconds(45)
 while([DateTime]::UtcNow -lt $deadline){Start-Sleep -Milliseconds 500;try{$status=Get-Content -LiteralPath (Join-Path $public 'status.json') -Raw | ConvertFrom-Json;if($status.At -ge $begin -and $status.Version -eq $m.Version -and $status.State -notin @('STARTING','ERROR_PROTECTION_UNCONFIRMED','STOPPED','STOPPED_INCOMPLETE')){$ok=$true;break}}catch{}}
 if(!$ok){throw 'Fresh service readiness heartbeat not confirmed'}
 [IO.File]::WriteAllText($floorFile,$m.Version.ToString());if($PublisherSha256){[IO.File]::WriteAllText($pinFile,$PublisherSha256.ToLowerInvariant())}
 New-Item -Path 'HKLM:\Software\SARGONService' -Force|Out-Null;New-ItemProperty -Path 'HKLM:\Software\SARGONService' -Name CurrentDirectory -Value $destination -PropertyType String -Force|Out-Null;New-ItemProperty -Path 'HKLM:\Software\SARGONService' -Name Version -Value $m.Version -PropertyType DWord -Force|Out-Null
 Write-Output 'Service registered; current heartbeat confirmed. This is not malware efficacy or full-system blocking validation. Existing GUI remains a separate manual review/quarantine tool.'
}catch{
 $reason=$_;Stop-OwnService
 if($oldPath){foreach($n in @('service-plan.json','settings.json')){$source=Join-Path $backup $n;if(Test-Path -LiteralPath $source){Copy-Item -LiteralPath $source -Destination (Join-Path $private $n) -Force}};$current=Get-CimInstance Win32_Service -Filter "Name='$name'";$rollback=Invoke-CimMethod -InputObject $current -MethodName Change -Arguments @{PathName=$oldPath};if($rollback.ReturnValue -eq 0){Start-Service $name;Write-Warning 'Previous service binary restored; inspect status and logs.'}}
 else{Sc -Arguments @('config',$name,'start=','demand')}
 throw $reason
}
