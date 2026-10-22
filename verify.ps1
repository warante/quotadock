$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'test.ps1')
& (Join-Path $PSScriptRoot 'release.ps1')
& (Join-Path $PSScriptRoot 'package-test.ps1')
& (Join-Path $PSScriptRoot 'privacy-check.ps1')
