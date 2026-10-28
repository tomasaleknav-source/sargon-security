#Requires -Version 5.1
[CmdletBinding()]param([Parameter(Mandatory=$true)][string]$CertificateThumbprint,[Parameter(Mandatory=$true)][uri]$TimestampServer,[string]$Package,[string]$MainBinary,[string]$Installer)
$ErrorActionPreference='Stop'
if($env:OS -ne 'Windows_NT'){throw 'Native Windows signing required; no signature was created'}
if($CertificateThumbprint -notmatch '^[a-fA-F0-9]{40}$' -or $TimestampServer.Scheme -ne 'https'){throw 'Explicit certificate thumbprint and HTTPS RFC3161 timestamp server required'}
if($Installer -and ($Package -or $MainBinary)){throw 'Sign installer separately AFTER signing and packaging payload'}
if(!$Installer -and (!$Package -or !$MainBinary)){throw 'Specify Package and MainBinary, or Installer'}
$tool=Get-Command signtool.exe -ErrorAction Stop
$cert=Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$CertificateThumbprint)
if(!$cert.HasPrivateKey -or $cert.NotBefore.ToUniversalTime() -gt [DateTime]::UtcNow -or $cert.NotAfter.ToUniversalTime() -le [DateTime]::UtcNow){throw 'Usable publisher certificate and private-key access required'}
if(!@($cert.EnhancedKeyUsageList|Where-Object {$_.ObjectId.Value -eq '1.3.6.1.5.5.7.3.3'}).Count){throw 'Certificate lacks code-signing usage'}
function Assert-NoLink([string]$Path){$full=[IO.Path]::GetFullPath($Path);while($full){if(Test-Path -LiteralPath $full){if((Get-Item -LiteralPath $full -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse path rejected'}};$full=[IO.Path]::GetDirectoryName($full)}}
function Sign-One([string]$File){Assert-NoLink $File;if(!(Test-Path -LiteralPath $File -PathType Leaf)){throw 'Expected binary missing'};& $tool.Source sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampServer.AbsoluteUri /td SHA256 $File;if($LASTEXITCODE -ne 0){throw 'Authenticode signing failed'};& $tool.Source verify /pa /all $File;if($LASTEXITCODE -ne 0){throw 'Authenticode verification failed'};$sig=Get-AuthenticodeSignature -LiteralPath $File;if($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Thumbprint -ine $CertificateThumbprint -or !$sig.TimeStamperCertificate){throw 'Publisher identity or timestamp not confirmed'}}
if($Installer){Sign-One $Installer;Write-Output 'Installer publisher signature and timestamp verified; native product acceptance is still separate';return}
Assert-NoLink $Package;Assert-NoLink $MainBinary
foreach($name in @('TelefonoSargasPC.exe','SARGON.Service.exe','SARGON.Setup.exe','SARGON.CaseReview.exe','SARGON.NativeAcceptance.exe')){Sign-One (Join-Path $Package $name)}
Copy-Item -LiteralPath (Join-Path $Package 'TelefonoSargasPC.exe') -Destination $MainBinary -Force
$manifestPath=Join-Path $Package 'release.json';Assert-NoLink $manifestPath;$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
foreach($f in $manifest.Files){if($f.Name -cnotmatch '^[A-Za-z0-9_.-]{1,100}$' -or $f.Name -in @('.','..')){throw 'Invalid manifest file'};$path=Join-Path $Package $f.Name;Assert-NoLink $path;$f.Hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
$manifest.Signature='';[IO.File]::WriteAllText($manifestPath,($manifest|ConvertTo-Json -Depth 6 -Compress),(New-Object Text.UTF8Encoding($false)))
& (Join-Path $PSScriptRoot 'Sign-ServiceRelease.ps1') -Package $Package -CertificateThumbprint $CertificateThumbprint
Write-Output 'Payload and manifest signed. Now build NSIS installer, then sign that installer separately. Establish publisher SHA256 pin independently; this script does not establish production trust or certification.'
