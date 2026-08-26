using System;
using System.Globalization;
using System.Reflection;
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
        private bool printSlipOnTerminal = true;
        private int lastErrorCode = 0;
        private string lastErrorDescription = "OK";

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
                switch (methodNum)
                {
                    case 0:
                        retValue = "0.5.1-payment-return-test";
                        return;
                    case 1:
                        EnsureLength(p, 7);
                        p[0] = "Unitodi P8 Bio via PBF/POSConnector";
                        p[1] = "Legacy BPO 2.x driver. Device test, payment and return are enabled. PBF host success codes 0 and 00 are accepted.";
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
                    case 18:
                        retValue = printSlipOnTerminal;
                        return;
                    default:
                        SetError(12000, "This monetary operation is not enabled in build 0.5.1-payment-return-test.");
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
                try { printSlipOnTerminal = Convert.ToBoolean(value, CultureInfo.InvariantCulture); }
                catch
                {
                    string s = ToText(value).Trim();
                    printSlipOnTerminal = s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("да", StringComparison.OrdinalIgnoreCase);
                }
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
                   "<Parameter Name=\"PrintSlipOnTerminal\" Caption=\"Terminal prints slip\" TypeValue=\"Boolean\" DefaultValue=\"" + (printSlipOnTerminal ? "true" : "false") + "\"/>" +
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

        private bool CardOperation(int operationCode, object[] p, bool requireOriginalRrn)
        {
            EnsureLength(p, 7);
            if (!ValidateRuntime()) { p[6] = lastErrorDescription; return false; }

            decimal amountRub;
            try { amountRub = Convert.ToDecimal(p[2], CultureInfo.InvariantCulture); }
            catch
            {
                SetError(10010, "Invalid card operation amount.");
                p[6] = lastErrorDescription;
                return false;
            }

            long amountKopecks;
            try
            {
                decimal kopecks = Decimal.Round(amountRub * 100m, 0, MidpointRounding.AwayFromZero);
                amountKopecks = Decimal.ToInt64(kopecks);
            }
            catch
            {
                SetError(10010, "Card operation amount is out of range.");
                p[6] = lastErrorDescription;
                return false;
            }

            if (amountKopecks <= 0)
            {
                SetError(10010, "Card operation amount must be greater than zero.");
                p[6] = lastErrorDescription;
                return false;
            }

            string originalRrn = requireOriginalRrn ? ToText(p[4]).Trim() : "";
            if (requireOriginalRrn && originalRrn.Length == 0)
            {
                SetError(10012, "Return requires the RRN of the original payment.");
                p[6] = lastErrorDescription;
                return false;
            }

            ExchangeResult result;
            bool ok = Exchange(operationCode, amountKopecks, originalRrn, out result);
            if (!ok)
            {
                p[6] = result.Slip.Length > 0 ? result.Slip : lastErrorDescription;
                return false;
            }

            if (result.Pan.Length > 0) p[1] = result.Pan;
            if (result.Rrn.Length > 0) p[4] = result.Rrn;
            if (result.AuthorizationCode.Length > 0) p[5] = result.AuthorizationCode;
            p[6] = result.Slip.Length > 0 ? result.Slip : result.Message;
            SetOk();
            return true;
        }

        private bool Exchange(int operationCode, long? amountKopecks, string originalRrn, out ExchangeResult result)
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

                int rc = ToInt(ComCall(pc, "Exchange", req, rsp, timeoutMs));
                result.ExchangeCode = rc;
                result.Status = SafeGetInt(rsp, "Status");
                result.ResponseCode = SafeGetString(rsp, "ResponseCodeHost").Trim();
                result.Message = SafeGetString(rsp, "TextResponse");
                result.Rrn = SafeGetString(rsp, "ReferenceNumber");
                result.AuthorizationCode = SafeGetString(rsp, "AuthorizationCode");
                result.Pan = SafeGetString(rsp, "PAN");
                result.Slip = SafeGetString(rsp, "ReceiptData");
                result.OperationResult = SafeGetString(rsp, "OperationResult");
                if (result.Slip.Length == 0) result.Slip = result.Message;

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

        private static bool IsHostSuccess(string code)
        {
            if (String.IsNullOrWhiteSpace(code)) return true;
            code = code.Trim();
            return code == "0" || code == "00";
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
            public string AuthorizationCode = "";
            public string Pan = "";
            public string Slip = "";
            public string OperationResult = "";
        }
    }
}
