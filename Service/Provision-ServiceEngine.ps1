#Requires -Version 5.1
#Requires -RunAsAdministrator
$ErrorActionPreference='Stop'
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$base=Join-Path $env:ProgramFiles 'SARGONService'
function Assert-NoLink([string]$Path){$p=[IO.Path]::GetFullPath($Path);while($p){if(Test-Path -LiteralPath $p){if((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse point rejected'}};$p=[IO.Path]::GetDirectoryName($p)}}
Assert-NoLink $base
if(Get-Service SARGONProtection -ErrorAction SilentlyContinue | Where-Object Status -eq Running){throw 'Stop SARGONProtection before preparing the engine'}
if(Test-Path -LiteralPath (Join-Path $base 'engine')){throw 'Engine already exists; do not overwrite it automatically'}
$stage=Join-Path $base ('engine-stage-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
try {
 $zip=Join-Path $stage 'engine.zip';$request=[Net.HttpWebRequest]::Create('https://www.clamav.net/downloads/production/clamav-1.4.6.win.x64.zip');$request.Timeout=30000;$request.ReadWriteTimeout=30000
 $response=$request.GetResponse()
 try{$inputStream=$response.GetResponseStream();$output=[IO.File]::Create($zip);try{$buffer=New-Object byte[] 65536;$total=0L;while(($count=$inputStream.Read($buffer,0,$buffer.Length)) -gt 0){$total+=$count;if($total -gt 251658240){throw 'Engine download exceeds bound'};$output.Write($buffer,0,$count)}}finally{$output.Dispose();$inputStream.Dispose()}}finally{$response.Dispose()}
 if((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne '57b6fd1d60cd87bafe800f97407ecdef0576d36b3900b8b7abcfbbabe88295fd'){throw 'Official ClamAV archive digest mismatch'}
 $expanded=Join-Path $stage 'expanded';Expand-Archive -LiteralPath $zip -DestinationPath $expanded
 $bin=Join-Path $expanded 'clamav-1.4.6.win.x64';if(!(Test-Path -LiteralPath (Join-Path $bin 'clamscan.exe')) -or !(Test-Path -LiteralPath (Join-Path $bin 'freshclam.exe'))){throw 'Required engine binaries missing'}
 $files=@(Get-ChildItem -LiteralPath $expanded -File -Recurse | ForEach-Object {Assert-NoLink $_.FullName;@{Name=$_.FullName.Substring($expanded.Length+1);Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
 if($files.Count -lt 2 -or $files.Count -gt 4096){throw 'Engine file count invalid'}
 Move-Item -LiteralPath $expanded -Destination (Join-Path $base 'engine')
 [IO.File]::WriteAllText((Join-Path $base 'engine-integrity.json'),(ConvertTo-Json -InputObject $files -Compress -Depth 4),(New-Object Text.UTF8Encoding($false)))
 Write-Output 'Official pinned archive installed. The dedicated service identity receives read access only. Database update remains a separate step.'
}finally{if(Test-Path -LiteralPath $stage){Remove-Item -LiteralPath $stage -Recurse -Force}}
