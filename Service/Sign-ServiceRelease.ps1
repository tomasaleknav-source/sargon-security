#Requires -Version 5.1
[CmdletBinding()]param([string]$Package=$PSScriptRoot,[Parameter(Mandatory=$true)][string]$CertificateThumbprint)
$ErrorActionPreference='Stop'
$cert=Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$CertificateThumbprint)
if(!$cert.HasPrivateKey){throw 'The signing certificate has no accessible private key'}
$m=Get-Content -LiteralPath (Join-Path $Package 'release.json') -Raw | ConvertFrom-Json
$names=[string[]]@($m.Files.Name);[Array]::Sort($names,[StringComparer]::Ordinal);$canonical="SARGON|$($m.Version)`n";$seen=@{}
foreach($n in $names){$f=@($m.Files|Where-Object {$_.Name -ceq $n})[0];if($n -cnotmatch '^[A-Za-z0-9_.-]{1,100}$' -or $n -in @('.','..') -or $seen.ContainsKey($n.ToLowerInvariant())){throw 'Invalid file list'};$seen[$n.ToLowerInvariant()]=$true;$actual=(Get-FileHash -LiteralPath (Join-Path $Package $n) -Algorithm SHA256).Hash.ToLowerInvariant();if($actual -ne $f.Hash.ToLowerInvariant()){throw 'Release file changed'};$canonical+="$n|$actual`n"}
$rsa=[Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
try{if($rsa.ExportParameters($false).Modulus.Length -lt 256){throw 'RSA key must be at least2048 bits'};$m.Signature=[Convert]::ToBase64String($rsa.SignData([Text.Encoding]::UTF8.GetBytes($canonical),[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1))}finally{$rsa.Dispose()}
[IO.File]::WriteAllBytes((Join-Path $Package 'publisher.cer'),$cert.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert))
[IO.File]::WriteAllText((Join-Path $Package 'release.json'),($m|ConvertTo-Json -Depth 6 -Compress),(New-Object Text.UTF8Encoding($false)))
Write-Output ('Publisher SHA256 pin (verify independently before first deployment): '+(Get-FileHash -LiteralPath (Join-Path $Package 'publisher.cer') -Algorithm SHA256).Hash.ToLowerInvariant())
