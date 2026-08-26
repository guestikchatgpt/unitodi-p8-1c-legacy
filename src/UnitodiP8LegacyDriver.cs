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
        void GetMethodName(Int32 methodNum, Int32 propAlias, [MarshalAs(UnmanagedType.BStr)] ref string methodName);
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
    public class Driver : IInitDone, ILanguageExtender
    {
        private const string ComponentName = "UnitodiP8Legacy";
        private const string PosConnectorProgId = "POSConnectorInterface-posconlib.1";
        private const string PacketProgId = "SAPacket-posconlib.1";

        private string terminalId = "94678638";
        private int timeoutMs = 180000;
        private bool printSlipOnTerminal = true;
        private int lastErrorCode = 0;
        private string lastErrorDescription = "OK";

        private static readonly string[] MethodEn = new string[]
        {
            "GetVersion",
            "GetDescription",
            "GetLastError",
            "GetParameters",
            "SetParameter",
            "Open",
            "Close",
            "DeviceTest",
            "GetAdditionalActions",
            "DoAdditionalAction",
            "PayByPaymentCard",
            "ReturnPaymentByPaymentCard",
            "CancelPaymentByPaymentCard",
            "AuthorisationByPaymentCard",
            "AuthConfirmationByPaymentCard",
            "CancelAuthorisationByPaymentCard",
            "EmergencyReversal",
            "Settlement",
            "PrintSlipOnTerminal"
        };

        private static readonly string[] MethodRu = new string[]
        {
            "ПолучитьНомерВерсии",
            "ПолучитьОписание",
            "ПолучитьОшибку",
            "ПолучитьПараметры",
            "УстановитьПараметр",
            "Подключить",
            "Отключить",
            "ТестУстройства",
            "ПолучитьДополнительныеДействия",
            "ВыполнитьДополнительноеДействие",
            "ОплатитьПлатежнойКартой",
            "ВернутьПлатежПоПлатежнойКарте",
            "ОтменитьПлатежПоПлатежнойКарте",
            "ПреавторизацияПоПлатежнойКарте",
            "ЗавершитьПреавторизациюПоПлатежнойКарте",
            "ОтменитьПреавторизациюПоПлатежнойКарте",
            "АварийнаяОтменаОперации",
            "ИтогиДняПоКартам",
            "ПечатьКвитанцийНаТерминале"
        };

        private static readonly int[] ParamCounts = new int[]
        {
            0, 7, 1, 1, 2, 1, 1, 2, 1, 1,
            7, 7, 7, 7, 7, 7, 1, 2, 0
        };

        public void Init(object connection)
        {
            SetOk();
        }

        public void Done()
        {
        }

        public void GetInfo(ref object[] info)
        {
            if (info != null && info.Length > 0)
                info[0] = 2000;
        }

        public void RegisterExtensionAs(ref string extensionName)
        {
            extensionName = ComponentName;
        }

        public void GetNProps(ref Int32 props)
        {
            props = 0;
        }

        public void FindProp(string propName, ref Int32 propNum)
        {
            propNum = -1;
        }

        public void GetPropName(Int32 propNum, Int32 propAlias, ref string propName)
        {
            propName = null;
        }

        public void GetPropVal(Int32 propNum, ref object propVal)
        {
            propVal = null;
        }

        public void SetPropVal(Int32 propNum, ref object propVal)
        {
        }

        public void IsPropReadable(Int32 propNum, ref bool propRead)
        {
            propRead = false;
        }

        public void IsPropWritable(Int32 propNum, ref bool propWrite)
        {
            propWrite = false;
        }

        public void GetNMethods(ref Int32 methods)
        {
            methods = MethodEn.Length;
        }

        public void FindMethod(string methodName, ref Int32 methodNum)
        {
            methodNum = -1;
            if (methodName == null)
                return;

            for (int i = 0; i < MethodEn.Length; i++)
            {
                if (string.Equals(MethodEn[i], methodName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(MethodRu[i], methodName, StringComparison.OrdinalIgnoreCase))
                {
                    methodNum = i;
                    return;
                }
            }
        }

        public void GetMethodName(Int32 methodNum, Int32 methodAlias, ref string methodName)
        {
            if (methodNum < 0 || methodNum >= MethodEn.Length)
            {
                methodName = null;
                return;
            }
            methodName = methodAlias == 0 ? MethodRu[methodNum] : MethodEn[methodNum];
        }

        public void GetNParams(Int32 methodNum, ref Int32 pParams)
        {
            pParams = (methodNum >= 0 && methodNum < ParamCounts.Length) ? ParamCounts[methodNum] : 0;
        }

        public void GetParamDefValue(Int32 methodNum, Int32 paramNum, ref object paramDefValue)
        {
            paramDefValue = null;
        }

        public void HasRetVal(Int32 methodNum, ref bool retValue)
        {
            retValue = true;
        }

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
                    case 0: // GetVersion
                        retValue = "0.3.0-ci";
                        return;

                    case 1: // GetDescription
                        EnsureLength(p, 7);
                        p[0] = "Unitodi P8 Bio через PBF/POSConnector";
                        p[1] = "Интеграционный драйвер для старой БПО 2.x. Использует установленный POSConnector/PbfProxy.";
                        p[2] = "ЭквайринговыйТерминал";
                        p[3] = 2002;
                        p[4] = true;
                        p[5] = IsPosConnectorAvailable();
                        p[6] = "";
                        retValue = true;
                        return;

                    case 2: // GetLastError
                        EnsureLength(p, 1);
                        p[0] = lastErrorDescription;
                        retValue = lastErrorCode;
                        return;

                    case 3: // GetParameters
                        EnsureLength(p, 1);
                        p[0] = BuildParametersXml();
                        retValue = true;
                        return;

                    case 4: // SetParameter
                        EnsureLength(p, 2);
                        retValue = SetParameterInternal(ToText(p[0]), p[1]);
                        return;

                    case 5: // Open
                        EnsureLength(p, 1);
                        if (!IsPosConnectorAvailable())
                        {
                            SetError(10001, "32-битный POSConnector COM не зарегистрирован.");
                            retValue = false;
                            return;
                        }
                        p[0] = "PBF:" + terminalId;
                        SetOk();
                        retValue = true;
                        return;

                    case 6: // Close
                        SetOk();
                        retValue = true;
                        return;

                    case 7: // DeviceTest(Description, DemoModeIsActivated)
                        EnsureLength(p, 2);
                        p[1] = "";
                        retValue = DeviceTestInternal(ref p[0]);
                        return;

                    case 8: // GetAdditionalActions
                        EnsureLength(p, 1);
                        p[0] = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Actions/>";
                        retValue = true;
                        return;

                    case 9: // DoAdditionalAction
                        SetOk();
                        retValue = true;
                        return;

                    case 10: // PayByPaymentCard
                        retValue = CardOperation(1, p, true, false, false);
                        return;

                    case 11: // ReturnPaymentByPaymentCard
                        retValue = CardOperation(29, p, false, true, false);
                        return;

                    case 12: // CancelPaymentByPaymentCard
                        retValue = CardOperation(4, p, false, true, true);
                        return;

                    case 13: // AuthorisationByPaymentCard
                        retValue = CardOperation(15, p, true, false, false);
                        return;

                    case 14: // AuthConfirmationByPaymentCard
                        retValue = CardOperation(16, p, false, true, false);
                        return;

                    case 15: // CancelAuthorisationByPaymentCard
                        retValue = CardOperation(17, p, false, true, false);
                        return;

                    case 16: // EmergencyReversal
                        retValue = SimpleOperation(53, null, null, null, out _dummyResult);
                        return;

                    case 17: // Settlement
                        EnsureLength(p, 2);
                        ExchangeResult settlement;
                        bool settlementOk = SimpleOperation(59, null, null, null, out settlement);
                        p[1] = settlementOk ? settlement.Slip : settlement.Message;
                        retValue = settlementOk;
                        return;

                    case 18: // PrintSlipOnTerminal
                        retValue = printSlipOnTerminal;
                        return;

                    default:
                        SetError(10000, "Неизвестный метод драйвера: " + methodNum.ToString(CultureInfo.InvariantCulture));
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

        private static ExchangeResult _dummyResult;

        private bool SetParameterInternal(string name, object value)
        {
            if (string.Equals(name, "TerminalID", StringComparison.OrdinalIgnoreCase))
            {
                terminalId = ToText(value).Trim();
                if (terminalId.Length == 0)
                {
                    SetError(10002, "TerminalID не может быть пустым.");
                    return false;
                }
                SetOk();
                return true;
            }

            if (string.Equals(name, "TimeoutMs", StringComparison.OrdinalIgnoreCase))
            {
                int parsed;
                if (!Int32.TryParse(ToText(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) || parsed < 1000)
                {
                    SetError(10003, "TimeoutMs должен быть целым числом не менее 1000.");
                    return false;
                }
                timeoutMs = parsed;
                SetOk();
                return true;
            }

            if (string.Equals(name, "PrintSlipOnTerminal", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    printSlipOnTerminal = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                    string s = ToText(value);
                    printSlipOnTerminal = s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("да", StringComparison.OrdinalIgnoreCase);
                }
                SetOk();
                return true;
            }

            SetError(10004, "Неизвестный параметр: " + name);
            return false;
        }

        private string BuildParametersXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   "<Settings><Page Caption=\"PBF / POSConnector\"><Group Caption=\"Подключение\">" +
                   "<Parameter Name=\"TerminalID\" Caption=\"Terminal ID\" TypeValue=\"String\" DefaultValue=\"" + XmlEscape(terminalId) + "\"/>" +
                   "<Parameter Name=\"TimeoutMs\" Caption=\"Таймаут операции, мс\" TypeValue=\"Number\" DefaultValue=\"" + timeoutMs.ToString(CultureInfo.InvariantCulture) + "\"/>" +
                   "<Parameter Name=\"PrintSlipOnTerminal\" Caption=\"Слип печатает терминал\" TypeValue=\"Boolean\" DefaultValue=\"" + (printSlipOnTerminal ? "true" : "false") + "\"/>" +
                   "</Group></Page></Settings>";
        }

        private bool DeviceTestInternal(ref object description)
        {
            ExchangeResult result;
            bool ok = SimpleOperation(26, null, null, null, out result);
            if (ok)
            {
                description = "Связь с Unitodi P8 Bio установлена. TID=" + terminalId +
                              "; ответ=" + result.ResponseCode +
                              "; " + result.Message;
                return true;
            }
            description = lastErrorDescription;
            return false;
        }

        private bool CardOperation(int operation, object[] p, bool newOperation, bool needsRrn, bool needsTrxId)
        {
            EnsureLength(p, 7);

            string cardNumber = ToText(p[1]);
            double amountRub = Convert.ToDouble(p[2], CultureInfo.InvariantCulture);
            string receiptNumber = ToText(p[3]);
            string rrn = ToText(p[4]);

            long amountKopecks = (long)Math.Round(amountRub * 100.0, MidpointRounding.AwayFromZero);
            if (amountKopecks <= 0)
            {
                SetError(10010, "Сумма операции должна быть больше нуля.");
                return false;
            }

            int? trxId = null;
            if (needsTrxId)
            {
                int parsedTrx = ExtractTrxId(receiptNumber);
                if (parsedTrx <= 0)
                {
                    SetError(10011, "Для отмены не найден PBF TrxID в НомерЧека. Отмена возможна для операций, проведенных этим драйвером.");
                    return false;
                }
                trxId = parsedTrx;
            }

            if (needsRrn && rrn.Length == 0)
            {
                SetError(10012, "Для операции требуется RRN исходной транзакции.");
                return false;
            }

            ExchangeResult result;
            bool ok = SimpleOperation(operation, amountKopecks, needsRrn ? rrn : null, trxId, out result);
            if (!ok)
            {
                p[6] = result.Slip.Length > 0 ? result.Slip : result.Message;
                return false;
            }

            if (result.Pan.Length > 0)
                p[1] = result.Pan;

            if (newOperation && result.TrxId > 0)
                p[3] = EmbedTrxId(receiptNumber, result.TrxId);

            if (result.Rrn.Length > 0)
                p[4] = result.Rrn;

            if (result.AuthorizationCode.Length > 0)
                p[5] = result.AuthorizationCode;

            p[6] = result.Slip;
            return true;
        }

        private bool SimpleOperation(int operation, long? amountKopecks, string rrn, int? trxId, out ExchangeResult result)
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
                    SetError(10020, "POSConnector x86 COM не зарегистрирован.");
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

                ComSet(req, "OperationCode", operation);
                ComSet(req, "TerminalID", terminalId);

                if (amountKopecks.HasValue)
                {
                    ComSet(req, "Amount", amountKopecks.Value.ToString(CultureInfo.InvariantCulture));
                    ComSet(req, "CurrencyCode", "643");
                }

                if (!String.IsNullOrEmpty(rrn))
                    ComSet(req, "ReferenceNumber", rrn);

                if (trxId.HasValue)
                    ComSet(req, "TrxID", trxId.Value);

                int rc = ToInt(ComCall(pc, "Exchange", req, rsp, timeoutMs));
                result.ExchangeCode = rc;
                result.Status = SafeGetInt(rsp, "Status");
                result.ResponseCode = SafeGetString(rsp, "ResponseCodeHost");
                result.Message = SafeGetString(rsp, "TextResponse");
                result.Rrn = SafeGetString(rsp, "ReferenceNumber");
                result.AuthorizationCode = SafeGetString(rsp, "AuthorizationCode");
                result.Pan = SafeGetString(rsp, "PAN");
                result.TrxId = SafeGetInt(rsp, "TrxID");
                result.Slip = SafeGetString(rsp, "ReceiptData");
                if (result.Slip.Length == 0)
                    result.Slip = result.Message;

                string connectorError = ToText(ComGet(pc, "ErrorDescription"));
                int connectorErrorCode = ToInt(ComGet(pc, "ErrorCode"));

                bool responseOk = rc == 0 &&
                                  (result.ResponseCode.Length == 0 || result.ResponseCode == "00") &&
                                  (result.Status == 1 || result.Status == Int32.MinValue);

                if (!responseOk)
                {
                    string msg = result.Message;
                    if (msg.Length == 0)
                        msg = connectorError;
                    if (msg.Length == 0)
                        msg = "Операция PBF завершилась с ошибкой.";
                    SetError(rc != 0 ? rc : (connectorErrorCode != 0 ? connectorErrorCode : 10021), msg);
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

        private bool IsPosConnectorAvailable()
        {
            try
            {
                Type t1 = Type.GetTypeFromProgID(PosConnectorProgId, false);
                Type t2 = Type.GetTypeFromProgID(PacketProgId, false);
                return t1 != null && t2 != null;
            }
            catch
            {
                return false;
            }
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
                throw new ArgumentException("Недостаточно параметров вызова: ожидалось " + count.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static void ReleaseCom(object obj)
        {
            if (obj == null) return;
            try
            {
                if (Marshal.IsComObject(obj))
                    Marshal.FinalReleaseComObject(obj);
            }
            catch { }
        }

        private void SetOk()
        {
            lastErrorCode = 0;
            lastErrorDescription = "OK";
        }

        private void SetError(int code, string description)
        {
            lastErrorCode = code;
            lastErrorDescription = description ?? "Ошибка";
        }

        private static string XmlEscape(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string EmbedTrxId(string receiptNumber, int trxId)
        {
            string clean = RemoveTrxMarker(receiptNumber);
            if (clean.Length == 0)
                return "PBFTRX=" + trxId.ToString(CultureInfo.InvariantCulture);
            return clean + "|PBFTRX=" + trxId.ToString(CultureInfo.InvariantCulture);
        }

        private static int ExtractTrxId(string receiptNumber)
        {
            if (String.IsNullOrEmpty(receiptNumber)) return 0;
            const string marker = "PBFTRX=";
            int pos = receiptNumber.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (pos < 0) return 0;
            string tail = receiptNumber.Substring(pos + marker.Length);
            int sep = tail.IndexOf('|');
            if (sep >= 0) tail = tail.Substring(0, sep);
            int value;
            return Int32.TryParse(tail.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private static string RemoveTrxMarker(string receiptNumber)
        {
            if (String.IsNullOrEmpty(receiptNumber)) return "";
            const string marker = "|PBFTRX=";
            int pos = receiptNumber.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0) return receiptNumber.Substring(0, pos);
            if (receiptNumber.StartsWith("PBFTRX=", StringComparison.OrdinalIgnoreCase)) return "";
            return receiptNumber;
        }

        private sealed class ExchangeResult
        {
            public int ExchangeCode = -1;
            public int Status = Int32.MinValue;
            public int TrxId = 0;
            public string ResponseCode = "";
            public string Message = "";
            public string Rrn = "";
            public string AuthorizationCode = "";
            public string Pan = "";
            public string Slip = "";
        }
    }
}
