$ErrorActionPreference = 'Stop'

$SourceDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$DllSource  = Join-Path $SourceDir 'UnitodiP8LegacyDriver.dll'
$InstallDir = 'C:\POSConnector\1CLegacyDriver'
$Framework  = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
$RegAsm     = Join-Path $Framework 'RegAsm.exe'
$Dll        = Join-Path $InstallDir 'UnitodiP8LegacyDriver.dll'
$Tlb        = Join-Path $InstallDir 'UnitodiP8LegacyDriver.tlb'

function Get-Reg32Value([Microsoft.Win32.RegistryHive]$Hive, [string]$SubKey, [string]$ValueName) {
    $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey($Hive, [Microsoft.Win32.RegistryView]::Registry32)
    try {
        $key = $base.OpenSubKey($SubKey)
        if ($null -eq $key) { return $null }
        try { return $key.GetValue($ValueName, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) }
        finally { $key.Dispose() }
    }
    finally { $base.Dispose() }
}

Write-Host '=== PRECHECK ==='
if (-not (Test-Path $RegAsm))    { throw "32-bit RegAsm.exe not found: $RegAsm" }
if (-not (Test-Path $DllSource)) { throw "Driver DLL not found next to install.ps1: $DllSource" }

$posDll = Get-Reg32Value ([Microsoft.Win32.RegistryHive]::ClassesRoot) 'CLSID\{CE1D5C7D-4E4A-408C-95A4-FF074D6A3E95}\InprocServer32' ''
if ([string]::IsNullOrWhiteSpace([string]$posDll) -or -not (Test-Path $posDll)) {
    throw 'Registered 32-bit POSConnector COM was not found.'
}
Write-Host "POSConnector x86: OK ($posDll)"

$pbfInstallDir = Get-Reg32Value ([Microsoft.Win32.RegistryHive]::LocalMachine) 'SOFTWARE\PBF\POSConnector' 'InstallDir'
if ([string]::IsNullOrWhiteSpace([string]$pbfInstallDir)) {
    throw '32-bit HKLM\SOFTWARE\PBF\POSConnector\InstallDir was not found.'
}
$clientIni = Join-Path $pbfInstallDir 'client.ini'
if (-not (Test-Path $clientIni)) { throw "client.ini not found: $clientIni" }
Write-Host "PBF config: OK ($clientIni)"

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item $DllSource $Dll -Force

Write-Host "`n=== REGISTER x86 COM ==="
& $RegAsm $Dll /nologo /codebase /tlb:$Tlb
if ($LASTEXITCODE -ne 0) { throw "RegAsm failed with exit code $LASTEXITCODE" }

$progIdClsid = Get-Reg32Value ([Microsoft.Win32.RegistryHive]::ClassesRoot) 'AddIn.UnitodiP8Legacy\CLSID' ''
if ([string]::IsNullOrWhiteSpace([string]$progIdClsid)) {
    throw 'AddIn.UnitodiP8Legacy was not registered in the 32-bit COM registry view.'
}

Write-Host "`n=== OK ==="
Write-Host "Driver DLL: $Dll"
Write-Host "ProgID: AddIn.UnitodiP8Legacy"
Write-Host 'Next step in 1C: add the driver and run DEVICE TEST only.'
