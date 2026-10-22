$ErrorActionPreference = 'Stop'
# Only report the file and category, never the matched credential.
$patterns = @{
    'API or GitHub token' = '(?:sk-(?:ant-|proj-)?[A-Za-z0-9_-]{16,}|gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})'
    'JWT token' = 'eyJ[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{16,}'
    'Private key' = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'
    'Email address' = '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'
    'Personal Windows path' = '[A-Za-z]:[\\/](?:Users|home)[\\/][A-Za-z0-9._-]+'
}
$sources = @('UsageTray.cs', 'Provider.cs', 'AssemblyInfo.cs', 'Installer.cs', 'Tests.cs', 'Uninstall.ps1', 'build.ps1', 'test.ps1', 'release.ps1', 'privacy-check.ps1', 'README.md', 'CHANGELOG.md', '.gitignore', 'ci/windows.yml')
$files = @($sources | ForEach-Object { Join-Path $PSScriptRoot $_ })
$release = Join-Path $PSScriptRoot 'dist\v1.0.0'
if (Test-Path $release) {
    $files += @(Get-ChildItem -LiteralPath $release -File | Where-Object { $_.Extension -ne '.zip' } | Select-Object -ExpandProperty FullName)
}
$failed = $false
foreach ($file in $files) {
    if (!(Test-Path -LiteralPath $file)) { throw "Missing audit input: $file" }
    $bytes = [IO.File]::ReadAllBytes($file)
    foreach ($encoding in @([Text.Encoding]::UTF8, [Text.Encoding]::Unicode)) {
        $text = $encoding.GetString($bytes)
        foreach ($rule in $patterns.Keys) {
            if ([regex]::IsMatch($text, $patterns[$rule])) {
                Write-Warning ((Split-Path $file -Leaf) + ': ' + $rule)
                $failed = $true
            }
        }
    }
}
if (Test-Path $release) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach ($zip in @(Get-ChildItem -LiteralPath $release -Filter '*.zip' -File)) {
        $archive = [IO.Compression.ZipFile]::OpenRead($zip.FullName)
        try {
            foreach ($entry in $archive.Entries) {
                if ($entry.FullName -notin @('QuotaDock.OpenAI.exe', 'QuotaDock.Claude.exe', 'README.md', 'CHANGELOG.md')) { throw "Unexpected ZIP entry: $($entry.FullName)" }
                $stream = $entry.Open()
                $memory = New-Object IO.MemoryStream
                try { $stream.CopyTo($memory); $bytes = $memory.ToArray() } finally { $stream.Dispose(); $memory.Dispose() }
                foreach ($encoding in @([Text.Encoding]::UTF8, [Text.Encoding]::Unicode)) {
                    foreach ($rule in $patterns.Keys) {
                        if ([regex]::IsMatch($encoding.GetString($bytes), $patterns[$rule])) {
                            Write-Warning ($zip.Name + '/' + $entry.FullName + ': ' + $rule)
                            $failed = $true
                        }
                    }
                }
            }
        } finally { $archive.Dispose() }
    }
}
if ($failed) { throw 'Privacy audit failed. Review findings before publishing.' }
Write-Host 'OK: no known secret patterns, email addresses or personal paths found in sources and release artifacts.'
