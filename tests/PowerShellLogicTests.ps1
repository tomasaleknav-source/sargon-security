param([string]$Source)
$ErrorActionPreference='Stop';$n=0
function Check([bool]$Condition){if(!$Condition){throw "PowerShell check $script:n"};$script:n++}
function Reject([scriptblock]$Work){$hit=$false;try{& $Work|Out-Null}catch{$hit=$true};Check $hit}
$t=$null;$e=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $Source 'Service/Manage-Service.ps1'),[ref]$t,[ref]$e)
foreach($name in @('Assert-NoLink','Verify-Package')){$fn=$ast.FindAll({param($a)$a -is [Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq $name},$false)[0];. ([scriptblock]::Create($fn.Extent.Text))}
$root=Join-Path ([IO.Path]::GetTempPath()) ('sargon-ps-test-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $root|Out-Null
try{
 $files=@();foreach($name in @('SARGON.Service.exe','TelefonoSargasPC.exe','TelefonoSargasPC.exe.config','initial.hdb','Provision-ServiceEngine.ps1')){[IO.File]::WriteAllText((Join-Path $root $name),'fixture only');$files+=@{Name=$name;Hash=(Get-FileHash -LiteralPath (Join-Path $root $name) -Algorithm SHA256).Hash.ToLowerInvariant()}}
 $manifest=@{Version=37;Files=$files;Signature=''};$manifestPath=Join-Path $root 'release.json';$manifest|ConvertTo-Json -Depth 6 -Compress|Set-Content -LiteralPath $manifestPath
 $PublisherSha256='';$AllowUnsignedBeta=$false;Reject {Verify-Package $root 37};$AllowUnsignedBeta=$true;Check ((Verify-Package $root 37).Version -eq 37);Reject {Verify-Package $root 38}
 [IO.File]::WriteAllText((Join-Path $root 'initial.hdb'),'tampered');Reject {Verify-Package $root 37};[IO.File]::WriteAllText((Join-Path $root 'initial.hdb'),'fixture only')
 $rsa=[Security.Cryptography.RSA]::Create(2048);$request=[Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=SARGON-PS-TEST-ONLY',$rsa,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1);$cert=$request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-1),[DateTimeOffset]::UtcNow.AddDays(1))
 $names=[string[]]@($files.Name);[Array]::Sort($names,[StringComparer]::Ordinal);$canonical="SARGON|37`n";foreach($name in $names){$f=@($files|Where-Object {$_.Name -ceq $name})[0];$canonical+="$name|$($f.Hash)`n"};$manifest.Signature=[Convert]::ToBase64String($rsa.SignData([Text.Encoding]::UTF8.GetBytes($canonical),[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1));$manifest|ConvertTo-Json -Depth 6 -Compress|Set-Content -LiteralPath $manifestPath
 [IO.File]::WriteAllBytes((Join-Path $root 'publisher.cer'),$cert.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert));$PublisherSha256=(Get-FileHash -LiteralPath (Join-Path $root 'publisher.cer') -Algorithm SHA256).Hash.ToLowerInvariant();Check ((Verify-Package $root 37).Version -eq 37);$PublisherSha256='0'*64;Reject {Verify-Package $root 37};$PublisherSha256='';$AllowUnsignedBeta=$true;$manifest.Files+=@{Name='../escape';Hash='0'*64};$manifest|ConvertTo-Json -Depth 6 -Compress|Set-Content -LiteralPath $manifestPath;Reject {Verify-Package $root 37};$cert.Dispose();$rsa.Dispose()
 Write-Output "PASS: $n PowerShell verifier checks (unsigned refusal, explicit test mode, digest tamper, version floor, real RSA pin/signature, bad pin and traversal). Linux PowerShell7; no Windows SCM/ACL execution."
}finally{Remove-Item -LiteralPath $root -Recurse -Force}
