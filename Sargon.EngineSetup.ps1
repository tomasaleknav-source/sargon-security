$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
if(-not [Environment]::Is64BitOperatingSystem){throw 'This engine requires Windows x64'}
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$base=Join-Path $env:LOCALAPPDATA 'TelefonoSargasPC\clamav'
$target=Join-Path $base 'clamav-1.4.6.win.x64'
if(Test-Path $target){throw 'Engine directory exists; use database update, or review it manually before reinstalling'}
New-Item -ItemType Directory -Force -Path $base | Out-Null
$stage=Join-Path $base ('setup-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
try {
 $zip=Join-Path $stage 'engine.zip'
 $request=[Net.HttpWebRequest]::Create('https://www.clamav.net/downloads/production/clamav-1.4.6.win.x64.zip')
 $request.Timeout=30000;$request.ReadWriteTimeout=30000
 $response=$request.GetResponse()
 try {
  $inputStream=$response.GetResponseStream();$output=[IO.File]::Create($zip)
  try {$buffer=New-Object byte[] 65536;$total=0L;while(($count=$inputStream.Read($buffer,0,$buffer.Length)) -gt 0){$total+=$count;if($total -gt 251658240){throw 'Download exceeds limit'};$output.Write($buffer,0,$count)}}
  finally {$output.Dispose();$inputStream.Dispose()}
 } finally {$response.Dispose()}
 $hash=(Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant()
 if($hash -ne '57b6fd1d60cd87bafe800f97407ecdef0576d36b3900b8b7abcfbbabe88295fd'){throw 'Publisher archive hash mismatch'}
 Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $stage 'expanded')
 $engine=Join-Path $stage 'expanded\clamav-1.4.6.win.x64'
 if(-not (Test-Path (Join-Path $engine 'clamscan.exe')) -or -not (Test-Path (Join-Path $engine 'freshclam.exe'))){throw 'Required engine files missing'}
 Move-Item -LiteralPath $engine -Destination $target
 New-Item -ItemType Directory -Force -Path (Join-Path $base 'database') | Out-Null
 Write-Output 'ClamAV 1.4.6 LTS engine archive verified and installed. Databases must be downloaded separately; real-time blocking is not enabled.'
} finally {
 if(Test-Path $stage){Remove-Item -LiteralPath $stage -Recurse -Force}
}
