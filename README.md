# Unitodi P8 Bio -> 1C Retail 2.2 legacy driver

Experimental x86 COM integration layer for old 1C Peripheral Equipment Library (BPO) 2.x.
It bridges the legacy 1C acquiring-terminal API to an installed PBF POSConnector COM API.

## Verified target environment

- 1C platform: 8.3.13.1690 x86, thin client.
- 1C Retail: 2.2.8.27.
- BPO: 2.0.5.23 generation.
- Unitodi P8 Bio through PbfProxy.
- PbfProxy TCP endpoint: 127.0.0.1:40101.
- POSConnector x86 COM tested with version 0.1.11.0.
- Terminal ID is configured locally and is never stored in this public repository.

Architecture:

```text
1cv8c.exe x86
    -> AddIn.UnitodiP8Legacy
    -> POSConnector.dll x86 (COM)
    -> PbfProxy
    -> Unitodi P8 Bio
```

The repository does not redistribute PBF/POSConnector binaries.

## v0.6.0 production-core scope

Implemented and field-tested integration paths:

| 1C operation | PBF OperationCode | State |
|---|---:|---|
| Device test | 26 | enabled |
| Payment | 1 | enabled |
| Return | 29 | enabled |
| Cancel payment | 4 when exact RRN+TrxID is journaled; otherwise 29 refund | enabled |
| Emergency reversal | 4 against the exact recent in-process sale only | enabled with hard safety guard |
| Settlement | 59 | enabled |
| Print approved bank slip | ReceiptData -> 1C fiscal printer | enabled |

Not enabled in this driver:

- preauthorization 15/16/17;
- unconditional last-operation reversal 53;
- SBP-specific operations 30/31/32/33.

Biometric/SBP behavior inside universal PBF payment operation 1 depends on terminal/PBF/acquirer software and is outside the legacy 1C method contract.

## Bank-slip printing

In integrated PBF mode the driver returns the PBF ReceiptData text to 1C and reports
PrintSlipOnTerminal = false. This makes old BPO print the approved bank slip through the
cash-register/fiscal-printer path instead of assuming that P8 printed it itself.

The legacy PrintSlipOnTerminal parameter is still accepted for compatibility with already
stored 1C settings, but the effective value is always false.

## Transaction safety

Successful sales are journaled locally in:

```text
%LOCALAPPDATA%\UnitodiP8Legacy\transactions.tsv
```

Diagnostic calls/results are written to:

```text
%LOCALAPPDATA%\UnitodiP8Legacy\driver.log
```

The journal stores RRN and PBF TrxID for new transactions.

CancelPaymentByPaymentCard uses real PBF Void operation 4 only when the exact original RRN
has a stored TrxID. If no TrxID is available, the driver uses the already verified addressable
refund operation 29. If an op=4 request was actually sent and then fails or times out, the
driver does not automatically issue a refund, avoiding a possible double reversal.

EmergencyReversal never calls unsafe VoidLastOperation/op=53. It is accepted only for an
exact sale completed by the same COM driver instance within five minutes and only when both
RRN and TrxID are known; otherwise it fails closed.

## Host response codes

PBF/host success codes observed in production include 0, 00 and 000. The driver accepts an
empty host response or a response consisting only of zeroes. Mixed/non-zero codes remain
errors.

## CI build

GitHub Actions builds with 32-bit .NET Framework csc:

```text
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
/platform:x86
/codepage:65001
```

Every feature/fix push produces the UnitodiP8Legacy-x86 artifact and runs smoke tests plus
32-bit COM registration metadata validation.

## Installation

Do not compile on the cashier workstation.

With all 1C processes closed, unpack the Actions artifact and run elevated PowerShell:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\install.ps1
```

The installer only replaces/registers AddIn.UnitodiP8Legacy. It does not change PbfProxy,
POSConnector or serial-port configuration.

Local equipment parameters:

- TerminalID = merchant terminal ID;
- TimeoutMs = 180000;
- PrintSlipOnTerminal is accepted for compatibility but forced off by v0.6.0.

## Uninstall

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\uninstall.ps1
```

This unregisters only AddIn.UnitodiP8Legacy and leaves the PBF stack untouched.
