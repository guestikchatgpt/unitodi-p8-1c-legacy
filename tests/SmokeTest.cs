using System;
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
        d.FindMethod("DeviceTest", ref method);
        if (method != 7) return Fail("DeviceTest lookup");

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

        object[] versionArgs = new object[0];
        object version = null;
        d.CallAsFunc(0, ref version, ref versionArgs);
        if (!object.Equals(version, "0.5.0-payment-return-test")) return Fail("version");

        object[] desc = new object[7];
        object result = null;
        d.CallAsFunc(1, ref result, ref desc);
        if (!(result is bool) || !(bool)result) return Fail("GetDescription result");
        if (!object.Equals(desc[2], "ЭквайринговыйТерминал")) return Fail("equipment type");
        if (!object.Equals(desc[3], 2002)) return Fail("interface revision");
        if (Convert.ToString(desc[1]).IndexOf("payment and return are enabled", StringComparison.OrdinalIgnoreCase) < 0)
            return Fail("description does not advertise enabled operations");

        // CI has no merchant POSConnector. These calls must reach the payment/return
        // implementation and fail on runtime validation, not on the disabled-operation guard.
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
