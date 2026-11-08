using System.Globalization;

namespace JipperKeyViewer.Render;


internal static class CountFormatter
{
    public static string Format(long value, bool thousandSeparator)
        => thousandSeparator
            ? value.ToString("N0", CultureInfo.CurrentCulture)
            : value.ToString(CultureInfo.CurrentCulture);
}
