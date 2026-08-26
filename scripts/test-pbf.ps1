param(
    [Parameter(Mandatory=$true)]
    [string]$TerminalID,
    [int]$TimeoutMs = 15000
)
$ErrorActionPreference = 'Stop'

if ([Environment]::Is64BitProcess) {
    throw 'Run this script in 32-bit Windows PowerShell (SysWOW64).'
}

$pc = New-Object -ComObject 'POSConnectorInterface-posconlib.1'
$req = New-Object -ComObject 'SAPacket-posconlib.1'
$rsp = New-Object -ComObject 'SAPacket-posconlib.1'
try {
    $init = $pc.InitResources()
    if ($init -ne 0) { throw "InitResources failed: $($pc.ErrorCode) $($pc.ErrorDescription)" }
    $req.OperationCode = 26
    $req.TerminalID = $TerminalID
    $rc = $pc.Exchange($req, $rsp, $TimeoutMs)
    [pscustomobject]@{
        ExchangeCode = $rc
        ErrorCode = $pc.ErrorCode
        ErrorDescription = $pc.ErrorDescription
        OperationCode = $rsp.OperationCode
        Status = $rsp.Status
        TerminalID = $rsp.TerminalID
        ResponseCodeHost = $rsp.ResponseCodeHost
        TextResponse = $rsp.TextResponse
        ReferenceNumber = $rsp.ReferenceNumber
    } | Format-List
    if ($rc -ne 0 -or $rsp.Status -ne 1 -or $rsp.ResponseCodeHost -ne '00') { exit 2 }
}
finally {
    try { $pc.FreeResources() } catch {}
    foreach ($o in @($rsp,$req,$pc)) {
        if ($null -ne $o -and [Runtime.InteropServices.Marshal]::IsComObject($o)) {
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($o)
        }
    }
}
