param([Parameter(Mandatory=$true)][string[]]$Files, [string]$Thumbprint = $env:QUOTADOCK_SIGNING_THUMBPRINT, [switch]$RequireSigned)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Thumbprint)) {
    if ($RequireSigned) { throw 'A trusted code-signing certificate is required.' }
    Write-Warning 'No signing certificate configured. Artifacts will be explicitly distributed as unsigned.'
    return
}
$certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$Thumbprint"
if (!$certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date)) { throw 'Missing private key or expired signing certificate.' }
if (@($certificate.EnhancedKeyUsageList | Where-Object { $_.ObjectId.Value -eq '1.3.6.1.5.5.7.3.3' }).Count -eq 0) { throw 'Certificate is not for code signing.' }
foreach ($file in $Files) {
    $signature = Set-AuthenticodeSignature -FilePath $file -Certificate $certificate -HashAlgorithm SHA256 -TimestampServer 'http://timestamp.digicert.com'
    if ($signature.Status -ne 'Valid') { throw "Signature not trusted: $(Split-Path $file -Leaf)" }
}
