param([string]$ReleaseDirectory)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
# Never print a matching secret: report only its object/file and category.
$rules = @{
    'Token' = '(?:oc_sk_[A-Za-z0-9_-]{16,}|sk-(?:ant-|proj-)?[A-Za-z0-9_-]{16,}|gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})'
    'JWT' = 'eyJ[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{16,}'
    'Private key' = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'
    'Personal email' = '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'
    'Personal path' = '[A-Za-z]:[\\/](?:Users|home)[\\/][A-Za-z0-9._-]+'
}
$script:findings = 0
function Inspect-Bytes([byte[]]$bytes, [string]$label) {
    foreach ($encoding in @([Text.Encoding]::UTF8, [Text.Encoding]::Unicode)) {
        $text = $encoding.GetString($bytes)
        # Public GitHub noreply commit identities are intentionally retained.
        $text = [regex]::Replace($text, '[A-Za-z0-9._%+-]+@users\.noreply\.github\.com', '<public-github-noreply>')
        foreach ($rule in $rules.Keys) {
            if ([regex]::IsMatch($text, $rules[$rule])) { Write-Warning "$label : $rule"; $script:findings++ }
        }
    }
}
function Read-GitObject([string]$id) {
    if ($id -notmatch '^[a-f0-9]{40,64}$') { throw 'Invalid Git object identifier.' }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = 'git'; $info.Arguments = "cat-file -p $id"; $info.WorkingDirectory = $PSScriptRoot
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    $memory = New-Object IO.MemoryStream
    try {
        [void]$process.Start(); $errorOutput = $process.StandardError.ReadToEndAsync()
        $process.StandardOutput.BaseStream.CopyTo($memory); $process.WaitForExit(); [void]$errorOutput.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw 'Could not inspect Git object.' }
        return ,$memory.ToArray()
    } finally { $process.Dispose(); $memory.Dispose() }
}
$objects = @(git -C $PSScriptRoot rev-list --objects --all --reflog)
if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate Git history.' }
$inspected = 0
foreach ($entry in $objects) {
    $parts = $entry -split ' ', 2
    if ($parts.Count -gt 1 -and $parts[1] -match '(^|[/\\])(?:auth\.json|\.credentials\.json|\.env(?:\.[^/\\]+)?|[^/\\]+\.(?:pfx|p12|key))$') {
        Write-Warning ('Sensitive file name in Git object ' + $parts[0]); $script:findings++
    }
    Inspect-Bytes ([Text.Encoding]::UTF8.GetBytes($entry)) 'Git object path'
    Inspect-Bytes (Read-GitObject $parts[0]) ('Git object ' + $parts[0])
    $inspected++
}
$assets = 0; $zipEntries = 0
if ($ReleaseDirectory) {
    foreach ($file in Get-ChildItem -LiteralPath $ReleaseDirectory -File -Recurse) {
        Inspect-Bytes ([IO.File]::ReadAllBytes($file.FullName)) ('Release file ' + $file.Name); $assets++
        if ($file.Extension -ne '.zip') { continue }
        $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
        try {
            foreach ($item in $archive.Entries) {
                if ($item.Length -gt 16777216) { throw 'Oversized release entry; manual review required.' }
                $stream = $item.Open(); $memory = New-Object IO.MemoryStream
                try { $stream.CopyTo($memory); Inspect-Bytes $memory.ToArray() ('ZIP entry ' + $item.FullName); $zipEntries++ }
                finally { $stream.Dispose(); $memory.Dispose() }
            }
        } finally { $archive.Dispose() }
    }
}
if ($script:findings) { throw "Privacy findings: $script:findings. Do not change repository visibility until reviewed." }
Write-Host "OK: $inspected Git objects (all refs and local reflogs), $assets release files, $zipEntries ZIP entries; no known private-data patterns found."
