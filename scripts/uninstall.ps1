$ErrorActionPreference = 'Stop'
$InstallDir = 'C:\POSConnector\1CLegacyDriver'
$Framework  = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
$RegAsm     = Join-Path $Framework 'RegAsm.exe'
$Dll        = Join-Path $InstallDir 'UnitodiP8LegacyDriver.dll'

if (Test-Path $Dll) {
    & $RegAsm $Dll /nologo /unregister
    if ($LASTEXITCODE -ne 0) { throw "RegAsm unregister failed with exit code $LASTEXITCODE" }
}
Write-Host 'AddIn.UnitodiP8Legacy registration removed. POSConnector/PbfProxy were not changed.'
