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
        if (method != 10) return Fail("Russian PayByPaymentCard lookup");

        object[] desc = new object[7];
        object result = null;
        d.CallAsFunc(1, ref result, ref desc);
        if (!(result is bool) || !(bool)result) return Fail("GetDescription result");
        if (!object.Equals(desc[2], "ЭквайринговыйТерминал")) return Fail("equipment type");
        if (!object.Equals(desc[3], 2002)) return Fail("interface revision");

        Console.WriteLine("Smoke tests passed.");
        return 0;
    }

    private static int Fail(string what)
    {
        Console.Error.WriteLine("Smoke test failed: " + what);
        return 1;
    }
}
