namespace ButlerDotNet.Utilities;

public static class UnitUtilities
{
    public static double CompressBytes(double bytes, out string unit)
    {
        var gbRange = Math.Pow(1024, 3);
        var mbRange = Math.Pow(1024, 2);
        const int kbRange = 1024;

        if (bytes < kbRange)
        {
            unit = "B";
            return double.Round(bytes, 2);
        }

        if (bytes < mbRange)
        {
            unit = "KiB";
            return double.Round(bytes / kbRange, 2);
        }

        if (bytes < gbRange)
        {
            unit = "MiB";
            return double.Round(bytes / mbRange, 2);
        }

        unit = "GiB";
        return double.Round(bytes / gbRange, 2);
    }
}
