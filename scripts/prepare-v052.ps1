$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force build | Out-Null

$sourcePath = 'src\UnitodiP8LegacyDriver-fixed.cs'
$sourceOut  = 'build\UnitodiP8LegacyDriver-v052.cs'
$testPath   = 'tests\SmokeTest.cs'
$testOut    = 'build\SmokeTest-v052.cs'

# Windows PowerShell 5.1 treats UTF-8 without BOM as the active ANSI code page
# when -Encoding is omitted. Read repository sources explicitly as UTF-8.
$src = Get-Content $sourcePath -Raw -Encoding UTF8

$src = $src.Replace(
    '0.5.1-payment-return-test',
    '0.5.3-payment-return-cancel-utf8-test')

$src = $src.Replace(
    'Legacy BPO 2.x driver. Device test, payment and return are enabled. PBF host success codes 0 and 00 are accepted.',
    'Legacy BPO 2.x driver. Device test, payment and return are enabled; 1C cancellation is routed to an RRN-addressed PBF refund. UTF-8 interface names preserved. PBF host success codes 0 and 00 are accepted.')

$oldSwitch = @'
                    case 11:
                        retValue = CardOperation(29, p, true);
                        return;
                    case 18:
'@

$newSwitch = @'
                    case 11:
                        retValue = CardOperation(29, p, true);
                        return;
                    case 12:
                        // Retail 2.2 uses CancelPaymentByPaymentCard for same-shift returns.
                        // PBF Void (operation 4) additionally requires PBF TrxID field 23,
                        // which the legacy 1C driver contract does not pass to us.
                        // Use the RRN-addressed PBF Return operation (29) instead of an
                        // unsafe last-operation reversal.
                        retValue = CardOperation(29, p, true);
                        return;
                    case 18:
'@

if (-not $src.Contains($oldSwitch)) {
    throw 'Could not find the payment/return switch block in source.'
}
$src = $src.Replace($oldSwitch, $newSwitch)
Set-Content -Path $sourceOut -Value $src -Encoding UTF8

$test = Get-Content $testPath -Raw -Encoding UTF8
$test = $test.Replace(
    '0.5.1-payment-return-test',
    '0.5.3-payment-return-cancel-utf8-test')

$anchor = @'
        object[] versionArgs = new object[0];
'@

$cancelLookup = @'
        method = -1;
        d.FindMethod("CancelPaymentByPaymentCard", ref method);
        if (method != 12) return Fail("cancel lookup");
        paramCount = 0;
        d.GetNParams(method, ref paramCount);
        if (paramCount != 7) return Fail("cancel parameter count");

        object[] versionArgs = new object[0];
'@

if (-not $test.Contains($anchor)) {
    throw 'Could not find version-test anchor.'
}
$test = $test.Replace($anchor, $cancelLookup)

$tailAnchor = @'
        Console.WriteLine("Smoke tests passed.");
'@

$cancelRuntime = @'
        object[] cancel = { "device", "", 1.23m, "receipt", "123456789012", "AUTH", "" };
        result = null;
        d.CallAsFunc(12, ref result, ref cancel);
        if (!(result is bool) || (bool)result) return Fail("cancel validation result");
        if (GetLastErrorCode(d) == 12000) return Fail("cancel still disabled");

        Console.WriteLine("Smoke tests passed.");
'@

if (-not $test.Contains($tailAnchor)) {
    throw 'Could not find smoke-test tail anchor.'
}
$test = $test.Replace($tailAnchor, $cancelRuntime)
Set-Content -Path $testOut -Value $test -Encoding UTF8

Write-Host "Prepared $sourceOut"
Write-Host "Prepared $testOut"
