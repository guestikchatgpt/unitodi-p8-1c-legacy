# Unitodi P8 Bio -> 1C Retail 2.2 legacy driver

Experimental x86 COM integration layer for old 1C Peripheral Equipment Library (BPO) 2.x.
It bridges the legacy 1C acquiring-terminal API to the already installed PBF `POSConnector` COM API.

## Target environment

Verified base environment before this driver is introduced:

- 1C platform: 8.3.13.1690 x86, thin client.
- 1C Retail: 2.2.8.27.
- BPO: 2.0.5.23 generation.
- Unitodi P8 Bio: USB virtual serial port COM18.
- PbfProxy: service running, TCP `127.0.0.1:40101`.
- POSConnector x86 COM: version 0.1.11.0.
- Terminal ID: `94678638`.

Safe PBF operation `26` (`TestConnection`) was verified end-to-end with:

- `Exchange rc = 0`
- `Status = 1`
- `ResponseCodeHost = 00`
- `TextResponse = Операция выполнена`

## Architecture

```text
1cv8c.exe x86
    -> AddIn.UnitodiP8Legacy
    -> POSConnector.dll x86 (COM)
    -> PbfProxy 127.0.0.1:40101
    -> COM18
    -> Unitodi P8 Bio
```

The repository does **not** redistribute PBF/POSConnector binaries. They must already be installed and configured.

## CI build

GitHub Actions builds with the 32-bit .NET Framework compiler:

```text
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
/platform:x86
/codepage:65001
```

Every push to `main`, every PR, and manual `workflow_dispatch` produces the artifact:

`UnitodiP8Legacy-x86.zip`

The job also runs source-level smoke tests and validates `RegAsm` COM registration metadata.

## Install on the cashier workstation

Do not compile on the cashier workstation.

Download the latest Actions artifact, unpack it, then run an elevated PowerShell:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\install.ps1
```

The installer only copies/registers `UnitodiP8LegacyDriver.dll`. It does not modify PbfProxy or the existing POSConnector installation.

Prerequisites checked by the installer:

- 32-bit POSConnector COM registration exists.
- `HKLM\SOFTWARE\PBF\POSConnector\InstallDir` exists in the 32-bit registry view.
- `client.ini` exists in that directory.

## First 1C test

Create the driver as a preinstalled local COM component:

- Equipment type: `Эквайринговый терминал`
- Name: `Unitodi P8 Bio (PBF legacy test)`
- Object identifier / ProgID: `AddIn.UnitodiP8Legacy`

Initial parameters:

- `TerminalID = 94678638`
- `TimeoutMs = 180000`
- `PrintSlipOnTerminal = true`

The first test in 1C must be **Device Test only**. It maps to PBF operation `26` and does not perform a payment.

Do not run a real payment from 1C until the exact old-BPO call contract and persistence of receipt/RRN/TrxID have been verified.

## Current PBF operation mapping

| 1C operation | PBF OperationCode |
|---|---:|
| Device test | 26 |
| Payment | 1 |
| Return | 29 |
| Cancel payment | 4 |
| Preauthorization | 15 |
| Preauthorization completion | 16 |
| Preauthorization cancellation | 17 |
| Emergency reversal | 53 |
| Settlement | 59 |

## Uninstall

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\uninstall.ps1
```

This unregisters only `AddIn.UnitodiP8Legacy` and leaves PBF/POSConnector unchanged.
