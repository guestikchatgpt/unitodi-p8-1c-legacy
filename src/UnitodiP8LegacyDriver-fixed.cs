using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[assembly: ComVisible(true)]

namespace UnitodiP8Legacy
{
    [ComVisible(true)]
    [Guid("AB634001-F13D-11D0-A459-004095E1DAEA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitDone
    {
        void Init([MarshalAs(UnmanagedType.IDispatch)] object connection);
        void Done();
        void GetInfo([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref object[] info);
    }

    [ComVisible(true)]
    [Guid("AB634003-F13D-11D0-A459-004095E1DAEA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ILanguageExtender
    {
        void RegisterExtensionAs([MarshalAs(UnmanagedType.BStr)] ref string extensionName);
        void GetNProps(ref Int32 props);
        void FindProp([MarshalAs(UnmanagedType.BStr)] string propName, ref Int32 propNum);
        void GetPropName(Int32 propNum, Int32 propAlias, [MarshalAs(UnmanagedType.BStr)] ref string propName);
        void GetPropVal(Int32 propNum, [MarshalAs(UnmanagedType.Struct)] ref object propVal);
        void SetPropVal(Int32 propNum, [MarshalAs(UnmanagedType.Struct)] ref object propVal);
        void IsPropReadable(Int32 propNum, ref bool propRead);
        void IsPropWritable(Int32 propNum, ref bool propWrite);
        void GetNMethods(ref Int32 methods);
        void FindMethod([MarshalAs(UnmanagedType.BStr)] string methodName, ref Int32 methodNum);
        void GetMethodName(Int32 methodNum, Int32 methodAlias, [MarshalAs(UnmanagedType.BStr)] ref string methodName);
        void GetNParams(Int32 methodNum, ref Int32 pParams);
        void GetParamDefValue(Int32 methodNum, Int32 paramNum, [MarshalAs(UnmanagedType.Struct)] ref object paramDefValue);
        void HasRetVal(Int32 methodNum, ref bool retValue);
        void CallAsProc(Int32 methodNum, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref object[] pParams);
        void CallAsFunc(Int32 methodNum, [MarshalAs(UnmanagedType.Struct)] ref object retValue,
            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref object[] pParams);
    }

    [ComVisible(true)]
    [Guid("3B959EAD-3E6C-49FE-B3AC-B22F3ECD38A9")]
    [ProgId("AddIn.UnitodiP8Legacy")]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class Driver : IInitDone, ILanguageExtender
    {
        private const string ComponentName = "UnitodiP8Legacy";
        private const string PosConnectorProgId = "POSConnectorInterface-posconlib.1";
        private const string PacketProgId = "SAPacket-posconlib.1";

        private string terminalId = "";
        private int timeoutMs = 180000;
        private bool printSlipOnTerminal = false;
        private int lastErrorCode = 0;
        private string lastErrorDescription = "OK";
        private string pendingSaleRrn = "";
        private string pendingSaleTrxId = "";
        private long pendingSaleAmountKopecks = 0;
        private DateTime pendingSaleAt = DateTime.MinValue;
        private static readonly object FileLock = new object();

        private static readonly string[] MethodEn =
        {
            "GetVersion", "GetDescription", "GetLastError", "GetParameters", "SetParameter",
            "Open", "Close", "DeviceTest", "GetAdditionalActions", "DoAdditionalAction",
            "PayByPaymentCard", "ReturnPaymentByPaymentCard", "CancelPaymentByPaymentCard",
            "AuthorisationByPaymentCard", "AuthConfirmationByPaymentCard",
            "CancelAuthorisationByPaymentCard", "EmergencyReversal", "Settlement",
            "PrintSlipOnTerminal"
        };

        private static readonly string[] MethodRu =
        {
            "ПолучитьНомерВерсии", "ПолучитьОписание", "ПолучитьОшибку", "ПолучитьПараметры", "УстановитьПараметр",
            "Подключить", "Отключить", "ТестУстройства", "ПолучитьДополнительныеДействия", "ВыполнитьДополнительноеДействие",
            "ОплатитьПлатежнойКартой", "ВернутьПлатежПоПлатежнойКарте", "ОтменитьПлатежПоПлатежнойКарте",
            "ПреавторизацияПоПлатежнойКарте", "ЗавершитьПреавторизациюПоПлатежнойКарте",
            "ОтменитьПреавторизациюПоПлатежнойКарте", "АварийнаяОтменаОперации", "ИтогиДняПоКартам",
            "ПечатьКвитанцийНаТерминале"
        };

        private static readonly int[] ParamCounts =
        {
            0, 7, 1, 1, 2, 1, 1, 2, 1, 1,
            7, 7, 7, 7, 7, 7, 1, 2, 0
        };

        public void Init(object connection) { SetOk(); }
        public void Done() { }
        public void GetInfo(ref object[] info) { if (info != null && info.Length > 0) info[0] = 2000; }
        public void RegisterExtensionAs(ref string extensionName) { extensionName = ComponentName; }
        public void GetNProps(ref Int32 props) { props = 0; }
        public void FindProp(string propName, ref Int32 propNum) { propNum = -1; }
        public void GetPropName(Int32 propNum, Int32 propAlias, ref string propName) { propName = null; }
        public void GetPropVal(Int32 propNum, ref object propVal) { propVal = null; }
        public void SetPropVal(Int32 propNum, ref object propVal) { }
        public void IsPropReadable(Int32 propNum, ref bool propRead) { propRead = false; }
        public void IsPropWritable(Int32 propNum, ref bool propWrite) { propWrite = false; }
        public void GetNMethods(ref Int32 methods) { methods = MethodEn.Length; }

        public void FindMethod(string methodName, ref Int32 methodNum)
        {
            methodNum = -1;
            if (methodName == null) return;
            for (int i = 0; i < MethodEn.Length; i++)
            {
                if (String.Equals(MethodEn[i], methodName, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(MethodRu[i], methodName, StringComparison.OrdinalIgnoreCase))
                {
                    methodNum = i;
                    return;
                }
            }
        }

        public void GetMethodName(Int32 methodNum, Int32 methodAlias, ref string methodName)
        {
            if (methodNum < 0 || methodNum >= MethodEn.Length) { methodName = null; return; }
            methodName = methodAlias == 0 ? MethodRu[methodNum] : MethodEn[methodNum];
        }

        public void GetNParams(Int32 methodNum, ref Int32 pParams)
        {
            pParams = methodNum >= 0 && methodNum < ParamCounts.Length ? ParamCounts[methodNum] : 0;
        }

        public void GetParamDefValue(Int32 methodNum, Int32 paramNum, ref object paramDefValue) { paramDefValue = null; }
        public void HasRetVal(Int32 methodNum, ref bool retValue) { retValue = true; }

        public void CallAsProc(Int32 methodNum, ref object[] pParams)
        {
            object ignored = null;
            CallAsFunc(methodNum, ref ignored, ref pParams);
        }

        public void CallAsFunc(Int32 methodNum, ref object retValue, ref object[] p)
        {
            try
            {
                if (methodNum == 10 || methodNum == 11 || methodNum == 12)
                    TraceCall(methodNum, p);

                switch (methodNum)
                {
                    case 0:
                        retValue = "0.6.0-production-core";
                        return;
                    case 1:
                        EnsureLength(p, 7);
                        p[0] = "Unitodi P8 Bio via PBF/POSConnector";
                        p[1] = "Legacy BPO 2.x driver. Payment, return, safe cancellation, settlement and bank-slip printing through 1C are enabled. RRN and PBF TrxID are journaled locally; emergency reversal uses only the exact in-process sale.";
                        p[2] = "ЭквайринговыйТерминал";
                        p[3] = 2002;
                        p[4] = true;
                        p[5] = IsPosConnectorAvailable();
                        p[6] = "";
                        retValue = true;
                        return;
                    case 2:
                        EnsureLength(p, 1);
                        p[0] = lastErrorDescription;
                        retValue = lastErrorCode;
                        return;
                    case 3:
                        EnsureLength(p, 1);
                        p[0] = BuildParametersXml();
                        retValue = true;
                        return;
                    case 4:
                        EnsureLength(p, 2);
                        retValue = SetParameterInternal(ToText(p[0]), p[1]);
                        return;
                    case 5:
                        EnsureLength(p, 1);
                        if (!ValidateRuntime()) { retValue = false; return; }
                        p[0] = "PBF:" + terminalId;
                        SetOk();
                        retValue = true;
                        return;
                    case 6:
                        SetOk();
                        retValue = true;
                        return;
                    case 7:
                        EnsureLength(p, 2);
                        p[1] = "";
                        retValue = DeviceTest(ref p[0]);
                        return;
                    case 8:
                        EnsureLength(p, 1);
                        p[0] = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Actions/>";
                        retValue = true;
                        return;
                    case 9:
                        SetOk();
                        retValue = true;
                        return;
                    case 10:
                        retValue = CardOperation(1, p, false);
                        return;
                    case 11:
                        retValue = CardOperation(29, p, true);
                        return;
                    case 12:
                        retValue = CancelPaymentOperation(p);
                        return;
                    case 16:
                        retValue = EmergencyReversalOperation(p);
                        return;
                    case 17:
                        retValue = SettlementOperation(p);
                        return;
                    case 18:
                        retValue = false;
                        return;
                    default:
                        SetError(12000, "This monetary operation is not enabled in build 0.6.0-production-core.");
                        retValue = false;
                        return;
                }
            }
            catch (Exception ex)
            {
                SetError(10999, ex.GetBaseException().Message);
                retValue = false;
            }
        }

        private bool SetParameterInternal(string name, object value)
        {
            if (String.Equals(name, "TerminalID", StringComparison.OrdinalIgnoreCase))
            {
                terminalId = ToText(value).Trim();
                if (terminalId.Length == 0) { SetError(10002, "TerminalID cannot be empty."); return false; }
                SetOk();
                return true;
            }
            if (String.Equals(name, "TimeoutMs", StringComparison.OrdinalIgnoreCase))
            {
                int parsed;
                if (!Int32.TryParse(ToText(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) || parsed < 1000)
                {
                    SetError(10003, "TimeoutMs must be an integer >= 1000.");
                    return false;
                }
                timeoutMs = parsed;
                SetOk();
                return true;
            }
            if (String.Equals(name, "PrintSlipOnTerminal", StringComparison.OrdinalIgnoreCase))
            {
                // Integrated PBF returns ReceiptData to 1C. The legacy BPO must print
                // that bank slip through the fiscal printer; the P8 does not print
                // approved slips itself in this mode. Accept old persisted values,
                // but force the effective capability to false.
                printSlipOnTerminal = false;
                SetOk();
                return true;
            }
            SetError(10004, "Unknown parameter: " + name);
            return false;
        }

        private string BuildParametersXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   "<Settings><Page Caption=\"PBF / POSConnector\"><Group Caption=\"Connection\">" +
                   "<Parameter Name=\"TerminalID\" Caption=\"Terminal ID\" TypeValue=\"String\" DefaultValue=\"\"/>" +
                   "<Parameter Name=\"TimeoutMs\" Caption=\"Operation timeout, ms\" TypeValue=\"Number\" DefaultValue=\"" + timeoutMs.ToString(CultureInfo.InvariantCulture) + "\"/>" +
                   "<Parameter Name=\"PrintSlipOnTerminal\" Caption=\"Terminal prints slip (forced off in integrated mode)\" TypeValue=\"Boolean\" DefaultValue=\"false\"/>" +
                   "</Group></Page></Settings>";
        }

        private bool ValidateRuntime()
        {
            if (!IsPosConnectorAvailable())
            {
                SetError(10001, "32-bit POSConnector COM is not registered.");
                return false;
            }
            if (String.IsNullOrWhiteSpace(terminalId))
            {
                SetError(10002, "TerminalID is not configured.");
                return false;
            }
            return true;
        }

        private bool DeviceTest(ref object description)
        {
            if (!ValidateRuntime()) { description = lastErrorDescription; return false; }
            ExchangeResult result;
            bool ok = Exchange(26, null, null, out result);
            if (ok)
            {
                description = "PBF connection OK; status=" + result.Status.ToString(CultureInfo.InvariantCulture) +
                              "; host=" + result.ResponseCode + "; " + result.Message;
                return true;
            }
            description = lastErrorDescription;
            return false;
        }

        private bool SettlementOperation(object[] p)
        {
            EnsureLength(p, 2);
            Trace("CALL method=17; p0=" + SafeLog(ToText(p[0])) + "; p1=" + SafeLog(ToText(p[1])));

            if (!ValidateRuntime())
            {
                p[1] = lastErrorDescription;
                return false;
            }

            ExchangeResult result;
            bool ok = Exchange(59, null, null, out result);
            if (!ok)
            {
                p[1] = result.Slip.Length > 0 ? result.Slip : lastErrorDescription;
                Trace("SETTLEMENT FAIL; error=" + SafeLog(lastErrorDescription));
                return false;
            }

            p[1] = result.Slip.Length > 0 ? result.Slip : result.Message;
            Trace("SETTLEMENT OK; status=" + result.Status.ToString(CultureInfo.InvariantCulture) +
                  "; host=" + SafeLog(result.ResponseCode) +
                  "; text=" + SafeLog(result.Message));
            SetOk();
            return true;
        }

        private bool CancelPaymentOperation(object[] p)
        {
            EnsureLength(p, 7);
            if (!ValidateRuntime()) { p[6] = lastErrorDescription; return false; }

            long amountKopecks;
            if (!TryGetAmountKopecks(p[2], out amountKopecks))
            {
                p[6] = lastErrorDescription;
                return false;
            }

            string receiptNumber = ToText(p[3]).Trim();
            string cardHint = NormalizePan(ToText(p[1]));
            string originalRrn = ToText(p[4]).Trim();

            if (originalRrn.Length == 0)
            {
                originalRrn = FindRecordedSaleRrn(receiptNumber, amountKopecks, cardHint);
                if (originalRrn.Length > 0)
                    p[4] = originalRrn;
            }

            if (originalRrn.Length == 0)
            {
                SetError(10012, "Cancel requires the original RRN and no unique matching sale was found.");
                p[6] = lastErrorDescription;
                return false;
            }

            string originalTrxId = FindSaleTrxIdByRrn(originalRrn);
            if (originalTrxId.Length == 0)
            {
                Trace("CANCEL fallback to refund op=29: no journaled TrxID for rrn=" + SafeLog(originalRrn));
                return CardOperation(29, p, true);
            }

            ExchangeResult result;
            bool ok = Exchange(4, null, originalRrn, originalTrxId, out result);
            if (!ok)
            {
                p[6] = result.Slip.Length > 0 ? result.Slip : lastErrorDescription;
                Trace("VOID FAIL op=4; rrn=" + SafeLog(originalRrn) +
                      "; trx=" + SafeLog(originalTrxId) +
                      "; error=" + SafeLog(lastErrorDescription));
                return false;
            }

            if (result.Rrn.Length > 0) p[4] = result.Rrn;
            if (result.AuthorizationCode.Length > 0) p[5] = result.AuthorizationCode;
            p[6] = result.Slip.Length > 0 ? result.Slip : result.Message;

            AppendJournal("VOID", receiptNumber, amountKopecks, result.Rrn,
                          result.AuthorizationCode, result.Pan, originalRrn,
                          result.TrxId.Length > 0 ? result.TrxId : originalTrxId);
            if (String.Equals(pendingSaleRrn, originalRrn, StringComparison.OrdinalIgnoreCase))
                ClearPendingSale();

            Trace("VOID OK op=4; rrn=" + SafeLog(originalRrn) +
                  "; trx=" + SafeLog(originalTrxId));
            SetOk();
            return true;
        }

        private bool EmergencyReversalOperation(object[] p)
        {
            EnsureLength(p, 1);
            Trace("CALL method=16 EmergencyReversal");

            if (!ValidateRuntime()) return false;

            if (pendingSaleRrn.Length == 0 || pendingSaleTrxId.Length == 0 ||
                pendingSaleAt == DateTime.MinValue ||
                DateTime.Now.Subtract(pendingSaleAt) > TimeSpan.FromMinutes(5))
            {
                SetError(10030, "Emergency reversal refused: no exact recent in-process sale with RRN and TrxID.");
                Trace("EMERGENCY REVERSAL BLOCKED: no safe pending sale");
                return false;
            }

            ExchangeResult result;
            bool ok = Exchange(4, null, pendingSaleRrn, pendingSaleTrxId, out result);
            if (!ok)
            {
                Trace("EMERGENCY VOID FAIL; rrn=" + SafeLog(pendingSaleRrn) +
                      "; trx=" + SafeLog(pendingSaleTrxId) +
                      "; error=" + SafeLog(lastErrorDescription));
                return false;
            }

            AppendJournal("EMERGENCY_VOID", "", pendingSaleAmountKopecks, result.Rrn,
                          result.AuthorizationCode, result.Pan, pendingSaleRrn,
                          result.TrxId.Length > 0 ? result.TrxId : pendingSaleTrxId);
            Trace("EMERGENCY VOID OK; rrn=" + SafeLog(pendingSaleRrn) +
                  "; trx=" + SafeLog(pendingSaleTrxId));
            ClearPendingSale();
            SetOk();
            return true;
        }

        private bool TryGetAmountKopecks(object value, out long amountKopecks)
        {
            amountKopecks = 0;
            decimal amountRub;
            try { amountRub = Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
            catch
            {
                SetError(10010, "Invalid card operation amount.");
                return false;
            }

            try
            {
                decimal kopecks = Decimal.Round(amountRub * 100m, 0, MidpointRounding.AwayFromZero);
                amountKopecks = Decimal.ToInt64(kopecks);
            }
            catch
            {
                SetError(10010, "Card operation amount is out of range.");
                return false;
            }

            if (amountKopecks <= 0)
            {
                SetError(10010, "Card operation amount must be greater than zero.");
                return false;
            }
            return true;
        }

        private void ClearPendingSale()
        {
            pendingSaleRrn = "";
            pendingSaleTrxId = "";
            pendingSaleAmountKopecks = 0;
            pendingSaleAt = DateTime.MinValue;
        }

        private bool CardOperation(int operationCode, object[] p, bool requireOriginalRrn)
        {
            EnsureLength(p, 7);
            if (!ValidateRuntime()) { p[6] = lastErrorDescription; return false; }

            long amountKopecks;
            if (!TryGetAmountKopecks(p[2], out amountKopecks))
            {
                p[6] = lastErrorDescription;
                return false;
            }

            string receiptNumber = ToText(p[3]).Trim();
            string cardHint = NormalizePan(ToText(p[1]));
            string originalRrn = requireOriginalRrn ? ToText(p[4]).Trim() : "";

            if (requireOriginalRrn && originalRrn.Length == 0)
            {
                originalRrn = FindRecordedSaleRrn(receiptNumber, amountKopecks, cardHint);
                if (originalRrn.Length > 0)
                {
                    p[4] = originalRrn;
                    Trace("RRN fallback matched local sale journal: receipt=" + SafeLog(receiptNumber) +
                          "; amount=" + amountKopecks.ToString(CultureInfo.InvariantCulture) +
                          "; rrn=" + SafeLog(originalRrn));
                }
            }

            if (requireOriginalRrn && originalRrn.Length == 0)
            {
                SetError(10012, "1C did not provide the original RRN and no unique matching sale was found in the local journal. Open the return from the original sales receipt.");
                p[6] = lastErrorDescription;
                Trace("RETURN BLOCKED: missing original RRN; receipt=" + SafeLog(receiptNumber) +
                      "; amount=" + amountKopecks.ToString(CultureInfo.InvariantCulture) +
                      "; card=" + SafeLog(cardHint));
                return false;
            }

            ExchangeResult result;
            bool ok = Exchange(operationCode, amountKopecks, originalRrn, out result);
            if (!ok)
            {
                p[6] = result.Slip.Length > 0 ? result.Slip : lastErrorDescription;
                Trace("CARD FAIL op=" + operationCode.ToString(CultureInfo.InvariantCulture) +
                      "; error=" + SafeLog(lastErrorDescription));
                return false;
            }

            if (result.Pan.Length > 0) p[1] = result.Pan;
            if (result.Rrn.Length > 0) p[4] = result.Rrn;
            if (result.AuthorizationCode.Length > 0) p[5] = result.AuthorizationCode;
            p[6] = result.Slip.Length > 0 ? result.Slip : result.Message;

            if (operationCode == 1)
            {
                AppendJournal("SALE", receiptNumber, amountKopecks, result.Rrn,
                              result.AuthorizationCode, result.Pan, "", result.TrxId);
                pendingSaleRrn = result.Rrn;
                pendingSaleTrxId = result.TrxId;
                pendingSaleAmountKopecks = amountKopecks;
                pendingSaleAt = DateTime.Now;
            }
            else if (operationCode == 29)
            {
                AppendJournal("RETURN", receiptNumber, amountKopecks, result.Rrn,
                              result.AuthorizationCode, result.Pan, originalRrn, result.TrxId);
                if (String.Equals(pendingSaleRrn, originalRrn, StringComparison.OrdinalIgnoreCase))
                    ClearPendingSale();
            }

            Trace("CARD OK op=" + operationCode.ToString(CultureInfo.InvariantCulture) +
                  "; receipt=" + SafeLog(receiptNumber) +
                  "; amount=" + amountKopecks.ToString(CultureInfo.InvariantCulture) +
                  "; rrn=" + SafeLog(result.Rrn) +
                  "; refRaw=" + SafeLog(result.ReferenceNumberRaw) +
                  "; auth=" + SafeLog(result.AuthorizationCode) +
                  "; trx=" + SafeLog(result.TrxId) +
                  "; entry=" + SafeLog(result.CardEntryMode) +
                  "; opResult=" + SafeLog(result.OperationResult) +
                  "; card=" + SafeLog(NormalizePan(result.Pan)));

            SetOk();
            return true;
        }

        private bool Exchange(int operationCode, long? amountKopecks, string originalRrn, out ExchangeResult result)
        {
            return Exchange(operationCode, amountKopecks, originalRrn, "", out result);
        }

        private bool Exchange(int operationCode, long? amountKopecks, string originalRrn, string originalTrxId, out ExchangeResult result)
        {
            result = new ExchangeResult();
            object pc = null;
            object req = null;
            object rsp = null;

            try
            {
                Type pcType = Type.GetTypeFromProgID(PosConnectorProgId, false);
                Type packetType = Type.GetTypeFromProgID(PacketProgId, false);
                if (pcType == null || packetType == null)
                {
                    SetError(10020, "POSConnector x86 COM is not registered.");
                    result.Message = lastErrorDescription;
                    return false;
                }

                pc = Activator.CreateInstance(pcType);
                req = Activator.CreateInstance(packetType);
                rsp = Activator.CreateInstance(packetType);

                int init = ToInt(ComCall(pc, "InitResources"));
                if (init != 0)
                {
                    string initDesc = ToText(ComGet(pc, "ErrorDescription"));
                    SetError(init, "POSConnector InitResources: " + initDesc);
                    result.Message = lastErrorDescription;
                    return false;
                }

                ComSet(req, "OperationCode", operationCode);
                ComSet(req, "TerminalID", terminalId);

                if (amountKopecks.HasValue)
                {
                    ComSet(req, "Amount", amountKopecks.Value.ToString(CultureInfo.InvariantCulture));
                    ComSet(req, "CurrencyCode", "643");
                }

                if (!String.IsNullOrWhiteSpace(originalRrn))
                    ComSet(req, "ReferenceNumber", originalRrn);
                if (!String.IsNullOrWhiteSpace(originalTrxId))
                    ComSet(req, "TrxID", originalTrxId);

                int rc = ToInt(ComCall(pc, "Exchange", req, rsp, timeoutMs));
                result.ExchangeCode = rc;
                result.Status = SafeGetInt(rsp, "Status");
                result.ResponseCode = SafeGetString(rsp, "ResponseCodeHost").Trim();
                result.Message = SafeGetString(rsp, "TextResponse");
                result.ReferenceNumberRaw = SafeGetString(rsp, "ReferenceNumber").Trim();
                result.Rrn = result.ReferenceNumberRaw;
                result.AuthorizationCode = SafeGetString(rsp, "AuthorizationCode");
                result.TrxId = SafeGetString(rsp, "TrxID").Trim();
                result.CardEntryMode = SafeGetString(rsp, "CardEntryMode").Trim();
                result.MerchantId = SafeGetString(rsp, "MerchantID").Trim();
                result.Pan = SafeGetString(rsp, "PAN");
                result.Slip = SafeGetString(rsp, "ReceiptData");
                result.OperationResult = SafeGetString(rsp, "OperationResult");
                if (result.Slip.Length == 0) result.Slip = result.Message;

                string slipRrn = ExtractRrnFromSlip(result.Slip);
                if (slipRrn.Length > 0)
                    result.Rrn = slipRrn;

                int connectorErrorCode = SafeGetInt(pc, "ErrorCode");
                string connectorError = SafeGetString(pc, "ErrorDescription");
                bool hostOk = IsHostSuccess(result.ResponseCode);
                bool statusOk = result.Status == 1 || result.Status == Int32.MinValue;
                bool responseOk = rc == 0 && hostOk && statusOk;

                if (!responseOk)
                {
                    string msg = result.Message;
                    if (msg.Length == 0) msg = connectorError;
                    if (msg.Length == 0) msg = "PBF operation failed.";
                    int code = rc != 0 ? rc : (connectorErrorCode != 0 && connectorErrorCode != Int32.MinValue ? connectorErrorCode : 10021);
                    SetError(code, "op=" + operationCode.ToString(CultureInfo.InvariantCulture) +
                                   "; rc=" + rc.ToString(CultureInfo.InvariantCulture) +
                                   "; status=" + result.Status.ToString(CultureInfo.InvariantCulture) +
                                   "; host=" + result.ResponseCode + "; " + msg);
                    return false;
                }

                SetOk();
                return true;
            }
            catch (Exception ex)
            {
                SetError(10022, ex.GetBaseException().Message);
                result.Message = lastErrorDescription;
                return false;
            }
            finally
            {
                if (pc != null)
                {
                    try { ComCall(pc, "FreeResources"); } catch { }
                }
                ReleaseCom(rsp);
                ReleaseCom(req);
                ReleaseCom(pc);
            }
        }

        private static string ExtractRrnFromSlip(string slip)
        {
            if (String.IsNullOrWhiteSpace(slip)) return "";
            try
            {
                Match m = Regex.Match(slip, @"(?im)\bRRN\b\s*[:#№\-]?\s*([0-9]{6,20})");
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            catch { }
            return "";
        }

        private static string GetDataDir()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (String.IsNullOrWhiteSpace(root))
                root = Path.GetTempPath();
            return Path.Combine(root, "UnitodiP8Legacy");
        }

        private static string GetJournalPath()
        {
            return Path.Combine(GetDataDir(), "transactions.tsv");
        }

        private static string GetTracePath()
        {
            return Path.Combine(GetDataDir(), "driver.log");
        }

        private static void EnsureDataDir()
        {
            Directory.CreateDirectory(GetDataDir());
        }

        private static string SafeLog(string value)
        {
            if (value == null) return "";
            return value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("|", "/");
        }

        private static string NormalizePan(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "";
            string s = value.Trim();
            string digits = Regex.Replace(s, @"\D", "");
            if (s.IndexOf('*') >= 0) return SafeLog(s);
            if (digits.Length >= 4) return "****" + digits.Substring(digits.Length - 4);
            return SafeLog(s);
        }

        private static void Trace(string message)
        {
            try
            {
                lock (FileLock)
                {
                    EnsureDataDir();
                    File.AppendAllText(GetTracePath(),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                        "\t" + SafeLog(message) + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
            catch { }
        }

        private static void TraceCall(int methodNum, object[] p)
        {
            try
            {
                string[] a = new string[7];
                for (int i = 0; i < a.Length; i++)
                    a[i] = p != null && p.Length > i ? SafeLog(ToText(p[i])) : "<missing>";
                a[1] = NormalizePan(a[1]);
                Trace("CALL method=" + methodNum.ToString(CultureInfo.InvariantCulture) +
                      "; p0=" + a[0] +
                      "; p1=" + a[1] +
                      "; p2=" + a[2] +
                      "; p3=" + a[3] +
                      "; p4=" + a[4] +
                      "; p5=" + a[5] +
                      "; p6=" + a[6]);
            }
            catch { }
        }

        private static void AppendJournal(string type, string receipt, long amountKopecks,
                                          string rrn, string auth, string pan, string originalRrn,
                                          string trxId)
        {
            try
            {
                string line = String.Join("\t", new string[]
                {
                    DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
                    SafeLog(type),
                    SafeLog(receipt),
                    amountKopecks.ToString(CultureInfo.InvariantCulture),
                    SafeLog(rrn),
                    SafeLog(auth),
                    SafeLog(NormalizePan(pan)),
                    SafeLog(originalRrn),
                    SafeLog(trxId)
                }) + Environment.NewLine;

                lock (FileLock)
                {
                    EnsureDataDir();
                    File.AppendAllText(GetJournalPath(), line, Encoding.UTF8);
                }
            }
            catch { }
        }

        private static string FindRecordedSaleRrn(string receipt, long amountKopecks, string cardHint)
        {
            try
            {
                string path = GetJournalPath();
                if (!File.Exists(path)) return "";

                string[] lines;
                lock (FileLock)
                    lines = File.ReadAllLines(path, Encoding.UTF8);

                HashSet<string> returned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] f = lines[i].Split('\t');
                    if (f.Length >= 8 && String.Equals(f[1], "RETURN", StringComparison.OrdinalIgnoreCase) &&
                        !String.IsNullOrWhiteSpace(f[7]))
                        returned.Add(f[7].Trim());
                }

                List<string> matches = new List<string>();
                string normalizedHint = NormalizePan(cardHint);
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string[] f = lines[i].Split('\t');
                    if (f.Length < 8 || !String.Equals(f[1], "SALE", StringComparison.OrdinalIgnoreCase))
                        continue;

                    long savedAmount;
                    if (!Int64.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out savedAmount) ||
                        savedAmount != amountKopecks)
                        continue;

                    string savedRrn = f[4].Trim();
                    if (savedRrn.Length == 0 || returned.Contains(savedRrn))
                        continue;

                    bool receiptMatch = receipt.Length > 0 && String.Equals(f[2].Trim(), receipt, StringComparison.OrdinalIgnoreCase);
                    bool cardMatch = normalizedHint.Length > 0 &&
                                     String.Equals(NormalizePan(f[6]), normalizedHint, StringComparison.OrdinalIgnoreCase);

                    if (receiptMatch || cardMatch)
                    {
                        if (!matches.Contains(savedRrn))
                            matches.Add(savedRrn);
                    }
                }

                return matches.Count == 1 ? matches[0] : "";
            }
            catch
            {
                return "";
            }
        }

        private static string FindSaleTrxIdByRrn(string rrn)
        {
            if (String.IsNullOrWhiteSpace(rrn)) return "";
            try
            {
                string path = GetJournalPath();
                if (!File.Exists(path)) return "";
                string[] lines;
                lock (FileLock)
                    lines = File.ReadAllLines(path, Encoding.UTF8);

                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string[] f = lines[i].Split('\t');
                    if (f.Length < 9 || !String.Equals(f[1], "SALE", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (String.Equals(f[4].Trim(), rrn.Trim(), StringComparison.OrdinalIgnoreCase))
                        return f[8].Trim();
                }
            }
            catch { }
            return "";
        }

        private static bool IsHostSuccess(string code)
        {
            if (String.IsNullOrWhiteSpace(code)) return true;
            code = code.Trim();
            for (int i = 0; i < code.Length; i++)
                if (code[i] != '0') return false;
            return code.Length > 0;
        }

        private static bool IsPosConnectorAvailable()
        {
            try
            {
                return Type.GetTypeFromProgID(PosConnectorProgId, false) != null &&
                       Type.GetTypeFromProgID(PacketProgId, false) != null;
            }
            catch { return false; }
        }

        private static object ComCall(object target, string name, params object[] args)
        {
            return target.GetType().InvokeMember(name,
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                null, target, args, CultureInfo.InvariantCulture);
        }

        private static object ComGet(object target, string name)
        {
            return target.GetType().InvokeMember(name,
                BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
                null, target, null, CultureInfo.InvariantCulture);
        }

        private static void ComSet(object target, string name, object value)
        {
            target.GetType().InvokeMember(name,
                BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance,
                null, target, new object[] { value }, CultureInfo.InvariantCulture);
        }

        private static string SafeGetString(object target, string name)
        {
            try { return ToText(ComGet(target, name)); }
            catch { return ""; }
        }

        private static int SafeGetInt(object target, string name)
        {
            try { return ToInt(ComGet(target, name)); }
            catch { return Int32.MinValue; }
        }

        private static int ToInt(object value)
        {
            if (value == null) return 0;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static string ToText(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        private static void EnsureLength(object[] p, int count)
        {
            if (p == null || p.Length < count)
                throw new ArgumentException("Expected at least " + count.ToString(CultureInfo.InvariantCulture) + " parameters.");
        }

        private static void ReleaseCom(object obj)
        {
            if (obj == null) return;
            try { if (Marshal.IsComObject(obj)) Marshal.FinalReleaseComObject(obj); }
            catch { }
        }

        private void SetOk() { lastErrorCode = 0; lastErrorDescription = "OK"; }
        private void SetError(int code, string description) { lastErrorCode = code; lastErrorDescription = description ?? "Error"; }

        private sealed class ExchangeResult
        {
            public int ExchangeCode = -1;
            public int Status = Int32.MinValue;
            public string ResponseCode = "";
            public string Message = "";
            public string Rrn = "";
            public string ReferenceNumberRaw = "";
            public string AuthorizationCode = "";
            public string TrxId = "";
            public string CardEntryMode = "";
            public string MerchantId = "";
            public string Pan = "";
            public string Slip = "";
            public string OperationResult = "";
        }
    }
}
