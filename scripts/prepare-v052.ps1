$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force build | Out-Null

$sourcePath = 'src\UnitodiP8LegacyDriver-fixed.cs'
$sourceOut  = 'build\UnitodiP8LegacyDriver-v052.cs'
$testPath   = 'tests\SmokeTest.cs'
$testOut    = 'build\SmokeTest-v052.cs'

$src = Get-Content $sourcePath -Raw -Encoding UTF8
$test = Get-Content $testPath -Raw -Encoding UTF8

if ($src -notmatch '0\.5\.4-rrn-journal-test') {
    throw 'Expected v0.5.4 source version marker was not found.'
}
if ($src -notmatch 'ПолучитьНомерВерсии') {
    throw 'UTF-8 Russian method names are missing from source.'
}
if ($src -notmatch 'case 12:') {
    throw 'CancelPaymentByPaymentCard implementation is missing.'
}

Set-Content -Path $sourceOut -Value $src -Encoding UTF8
Set-Content -Path $testOut -Value $test -Encoding UTF8

Write-Host "Prepared $sourceOut"
Write-Host "Prepared $testOut"
