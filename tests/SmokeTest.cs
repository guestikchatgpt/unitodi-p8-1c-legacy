using System;
using System.Reflection;
using UnitodiP8Legacy;

internal static class SmokeTest
{
    public static int Main()
    {
        var d = new Driver();

        string extension = null;
        d.RegisterExtensionAs(ref extension);
        if (extension != "UnitodiP8Legacy") return Fail("extension name");

        int n = 0;
        d.GetNMethods(ref n);
        if (n != 19) return Fail("method count");

        int method = -1;
        d.FindMethod("ПолучитьНомерВерсии", ref method);
        if (method != 0) return Fail("russian GetVersion lookup");

        method = -1;
        d.FindMethod("ОплатитьПлатежнойКартой", ref method);
        if (method != 10) return Fail("payment lookup");
        int paramCount = 0;
        d.GetNParams(method, ref paramCount);
        if (paramCount != 7) return Fail("payment parameter count");

        method = -1;
        d.FindMethod("ВернутьПлатежПоПлатежнойКарте", ref method);
        if (method != 11) return Fail("return lookup");
        paramCount = 0;
        d.GetNParams(method, ref paramCount);
        if (paramCount != 7) return Fail("return parameter count");

        method = -1;
        d.FindMethod("ОтменитьПлатежПоПлатежнойКарте", ref method);
        if (method != 12) return Fail("cancel lookup");
        paramCount = 0;
        d.GetNParams(method, ref paramCount);
        if (paramCount != 7) return Fail("cancel parameter count");

        method = -1;
        d.FindMethod("АварийнаяОтменаОперации", ref method);
        if (method != 16) return Fail("emergency reversal lookup");
        paramCount = 0;
        d.GetNParams(method, ref paramCount);
        if (paramCount != 1) return Fail("emergency reversal parameter count");

        method = -1;
        d.FindMethod("ИтогиДняПоКартам", ref method);
        if (method != 17) return Fail("settlement lookup");
        paramCount = 0;
        d.GetNParams(method, ref paramCount);
        if (paramCount != 2) return Fail("settlement parameter count");

        object[] versionArgs = new object[0];
        object version = null;
        d.CallAsFunc(0, ref version, ref versionArgs);
        if (!object.Equals(version, "0.6.0-production-core")) return Fail("version");

        object[] desc = new object[7];
        object result = null;
        d.CallAsFunc(1, ref result, ref desc);
        if (!(result is bool) || !(bool)result) return Fail("GetDescription result");
        if (!object.Equals(desc[2], "ЭквайринговыйТерминал")) return Fail("equipment type");
        if (!object.Equals(desc[3], 2002)) return Fail("interface revision");
        if (Convert.ToString(desc[1]).IndexOf("bank-slip printing through 1C", StringComparison.OrdinalIgnoreCase) < 0)
            return Fail("description does not advertise 1C slip printing");

        MethodInfo hostSuccess = typeof(Driver).GetMethod("IsHostSuccess", BindingFlags.NonPublic | BindingFlags.Static);
        if (hostSuccess == null) return Fail("IsHostSuccess missing");
        if (!(bool)hostSuccess.Invoke(null, new object[] { "0" })) return Fail("host code 0 rejected");
        if (!(bool)hostSuccess.Invoke(null, new object[] { "00" })) return Fail("host code 00 rejected");
        if (!(bool)hostSuccess.Invoke(null, new object[] { "000" })) return Fail("host code 000 rejected");
        if (!(bool)hostSuccess.Invoke(null, new object[] { "0000" })) return Fail("host code 0000 rejected");
        if (!(bool)hostSuccess.Invoke(null, new object[] { "" })) return Fail("empty host code rejected");
        if ((bool)hostSuccess.Invoke(null, new object[] { "05" })) return Fail("host error code accepted");
        if ((bool)hostSuccess.Invoke(null, new object[] { "0005" })) return Fail("mixed host error code accepted");

        MethodInfo trxLookup = typeof(Driver).GetMethod("FindSaleTrxIdByRrn", BindingFlags.NonPublic | BindingFlags.Static);
        if (trxLookup == null) return Fail("FindSaleTrxIdByRrn missing");

        MethodInfo extract = typeof(Driver).GetMethod("ExtractRrnFromSlip", BindingFlags.NonPublic | BindingFlags.Static);
        if (extract == null) return Fail("ExtractRrnFromSlip missing");
        string rrn = (string)extract.Invoke(null, new object[] {
            "ОПЛАТА\r\nНОМЕР ССЫЛКИ RRN: 018208579376\r\nКОД АВТОРИЗАЦИИ:217256"
        });
        if (rrn != "018208579376") return Fail("RRN extraction");

        object[] pay = { "device", "", 1.23m, "receipt", "", "", "" };
        result = null;
        d.CallAsFunc(10, ref result, ref pay);
        if (!(result is bool) || (bool)result) return Fail("payment validation result");
        if (GetLastErrorCode(d) == 12000) return Fail("payment still disabled");

        object[] ret = { "device", "", 1.23m, "receipt", "123456789012", "AUTH", "" };
        result = null;
        d.CallAsFunc(11, ref result, ref ret);
        if (!(result is bool) || (bool)result) return Fail("return validation result");
        if (GetLastErrorCode(d) == 12000) return Fail("return still disabled");

        object[] cancel = { "device", "", 1.23m, "receipt", "123456789012", "AUTH", "" };
        result = null;
        d.CallAsFunc(12, ref result, ref cancel);
        if (!(result is bool) || (bool)result) return Fail("cancel validation result");
        if (GetLastErrorCode(d) == 12000) return Fail("cancel still disabled");

        object[] emergency = { "device" };
        result = null;
        d.CallAsFunc(16, ref result, ref emergency);
        if (!(result is bool) || (bool)result) return Fail("emergency validation result");
        if (GetLastErrorCode(d) == 12000) return Fail("emergency reversal still disabled");

        object[] settlement = { "device", "" };
        result = null;
        d.CallAsFunc(17, ref result, ref settlement);
        if (!(result is bool) || (bool)result) return Fail("settlement validation result");
        if (GetLastErrorCode(d) == 12000) return Fail("settlement still disabled");

        object[] printArgs = new object[0];
        result = null;
        d.CallAsFunc(18, ref result, ref printArgs);
        if (!(result is bool) || (bool)result) return Fail("PrintSlipOnTerminal must be false");

        Console.WriteLine("Smoke tests passed.");
        return 0;
    }

    private static int GetLastErrorCode(Driver d)
    {
        object[] args = new object[1];
        object result = null;
        d.CallAsFunc(2, ref result, ref args);
        return Convert.ToInt32(result);
    }

    private static int Fail(string what)
    {
        Console.Error.WriteLine("Smoke test failed: " + what);
        return 1;
    }
}
