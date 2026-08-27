$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force build | Out-Null

$sourcePath = 'src\UnitodiP8LegacyDriver-fixed.cs'
$sourceOut  = 'build\UnitodiP8LegacyDriver-v052.cs'
$testPath   = 'tests\SmokeTest.cs'
$testOut    = 'build\SmokeTest-v052.cs'

$src = Get-Content $sourcePath -Raw -Encoding UTF8
$test = Get-Content $testPath -Raw -Encoding UTF8

if ($src -notmatch '0\.6\.0-production-core') {
    throw 'Expected v0.6.0 source version marker was not found.'
}
if ($src -notmatch 'case 12:') {
    throw 'CancelPaymentByPaymentCard implementation is missing.'
}
if ($src -notmatch 'ExtractRrnFromSlip') {
    throw 'RRN extraction helper is missing.'
}
if ($src -notmatch 'FindRecordedSaleRrn') {
    throw 'RRN journal fallback helper is missing.'
}
if ($src -notmatch 'SettlementOperation') {
    throw 'Settlement implementation is missing.'
}
if ($src -notmatch 'Exchange\(59, null, null') {
    throw 'PBF settlement operation 59 mapping is missing.'
}
if ($src -notmatch 'case 12:[\s\S]*CardOperation\(29, p, true\)') {
    throw 'Retail 2.2 method 12 must keep the verified PBF refund mapping.'
}
if ($src -notmatch 'EmergencyReversalOperation') {
    throw 'Safe emergency reversal implementation is missing.'
}
if ($src -notmatch 'ComSet\(req, "TrxID"') {
    throw 'PBF TrxID request mapping is missing.'
}
if ($src -notmatch 'SafeGetString\(rsp, "TrxID"') {
    throw 'PBF TrxID response mapping is missing.'
}
if ($src -notmatch 'case 18:[\s\S]*retValue = false;') {
    throw '1C bank-slip printing mode is not forced.'
}

Set-Content -Path $sourceOut -Value $src -Encoding UTF8
Set-Content -Path $testOut -Value $test -Encoding UTF8

Write-Host "Prepared $sourceOut"
Write-Host "Prepared $testOut"
