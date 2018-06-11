using System.Globalization;

namespace SharpStrike
{
    public static class FloatExtension
    {
        public static string ToSafeString(this float f)
        {
            return f.ToString(CultureInfo.InvariantCulture);
        }
    }
}