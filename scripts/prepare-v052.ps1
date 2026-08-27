$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force build | Out-Null

$sourcePath = 'src\UnitodiP8LegacyDriver-fixed.cs'
$sourceOut  = 'build\UnitodiP8LegacyDriver-v052.cs'
$testPath   = 'tests\SmokeTest.cs'
$testOut    = 'build\SmokeTest-v052.cs'

$src = Get-Content $sourcePath -Raw -Encoding UTF8
$test = Get-Content $testPath -Raw -Encoding UTF8

if ($src -notmatch '0\.5\.6-host-zeroes-test') {
    throw 'Expected v0.5.6 source version marker was not found.'
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

Set-Content -Path $sourceOut -Value $src -Encoding UTF8
Set-Content -Path $testOut -Value $test -Encoding UTF8

Write-Host "Prepared $sourceOut"
Write-Host "Prepared $testOut"
