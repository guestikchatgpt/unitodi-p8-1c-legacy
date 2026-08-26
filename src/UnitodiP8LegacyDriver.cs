using System;
using System.Globalization;
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

        // Merchant-specific values are deliberately not compiled into the public binary.
        private string terminalId = "";
        private int timeoutMs = 180000;
        private bool printSlipOnTerminal = true;
        private int lastErrorCode;
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
                        retValue = "0.4.0-test";
                        return;
                    case 1:
                        EnsureLength(p, 7);
                        p[0] = "Unitodi P8 Bio via PBF/POSConnector";
                        p[1] = "Legacy BPO 2.x compatibility driver; payment operations are disabled in this test build.";
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
                        if (!IsPosConnectorAvailable())
                        {
                            SetError(10001, "32-bit POSConnector COM is not registered.");
                            retValue = false;
                            return;
                        }
                        if (String.IsNullOrWhiteSpace(terminalId))
                        {
                            SetError(10002, "TerminalID is not configured.");
                            retValue = false;
                            return;
                        }
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
                    case 18:
                        retValue = printSlipOnTerminal;
                        return;
                    default:
                        SetError(12000, "Payment operations are disabled in the current test build.");
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
                string s = ToText(value).Trim();
                printSlipOnTerminal = s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("да", StringComparison.OrdinalIgnoreCase);
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

        private bool DeviceTest(ref object description)
        {
            if (String.IsNullOrWhiteSpace(terminalId))
            {
                SetError(10002, "TerminalID is not configured.");
                description = lastErrorDescription;
                return false;
            }

            object pc = null, req = null, rsp = null;
            try
            {
                Type pcType = Type.GetTypeFromProgID(PosConnectorProgId, true);
                Type packetType = Type.GetTypeFromProgID(PacketProgId, true);
                pc = Activator.CreateInstance(pcType);
                req = Activator.CreateInstance(packetType);
                rsp = Activator.CreateInstance(packetType);

                dynamic dpc = pc;
                dynamic dreq = req;
                dynamic drsp = rsp;

                int init = dpc.InitResources();
                if (init != 0)
                {
                    SetError((int)dpc.ErrorCode, "POSConnector InitResources: " + ToText(dpc.ErrorDescription));
                    description = lastErrorDescription;
                    return false;
                }

                dreq.OperationCode = 26;
                dreq.TerminalID = terminalId;
                int rc = dpc.Exchange(req, rsp, timeoutMs);
                int status = SafeInt(drsp, "Status", Int32.MinValue);
                string host = SafeString(drsp, "ResponseCodeHost");
                string text = SafeString(drsp, "TextResponse");

                if (rc == 0 && status == 1 && (host == "" || host == "00"))
                {
                    SetOk();
                    description = "PBF connection OK; status=1; host=" + host + "; " + text;
                    return true;
                }

                SetError(rc != 0 ? rc : 10100, "rc=" + rc.ToString(CultureInfo.InvariantCulture) + "; status=" + status.ToString(CultureInfo.InvariantCulture) + "; host=" + host + "; " + text);
                description = lastErrorDescription;
                return false;
            }
            catch (Exception ex)
            {
                SetError(10998, ex.GetBaseException().Message);
                description = lastErrorDescription;
                return false;
            }
            finally
            {
                try { if (pc != null) { dynamic dpc = pc; dpc.FreeResources(); } } catch { }
                ReleaseCom(rsp); ReleaseCom(req); ReleaseCom(pc);
            }
        }

        private static bool IsPosConnectorAvailable()
        {
            try { return Type.GetTypeFromProgID(PosConnectorProgId, false) != null; }
            catch { return false; }
        }

        private static int SafeInt(dynamic obj, string name, int fallback)
        {
            try
            {
                object v = obj.GetType().InvokeMember(name, System.Reflection.BindingFlags.GetProperty, null, obj, null);
                return Convert.ToInt32(v, CultureInfo.InvariantCulture);
            }
            catch { return fallback; }
        }

        private static string SafeString(dynamic obj, string name)
        {
            try
            {
                object v = obj.GetType().InvokeMember(name, System.Reflection.BindingFlags.GetProperty, null, obj, null);
                return ToText(v);
            }
            catch { return ""; }
        }

        private static void EnsureLength(object[] p, int count)
        {
            if (p == null || p.Length < count) throw new ArgumentException("Expected at least " + count.ToString(CultureInfo.InvariantCulture) + " parameters.");
        }

        private static string ToText(object value) { return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; }

        private static void ReleaseCom(object obj)
        {
            if (obj == null) return;
            try { if (Marshal.IsComObject(obj)) Marshal.FinalReleaseComObject(obj); }
            catch { }
        }

        private void SetOk() { lastErrorCode = 0; lastErrorDescription = "OK"; }
        private void SetError(int code, string description) { lastErrorCode = code; lastErrorDescription = description ?? "Error"; }
    }
}
