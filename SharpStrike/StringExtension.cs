using System;
using System.Globalization;

namespace SharpStrike
{
    public static class StringExtension
    {
        public static float ToSafeFloat(this string s)
        {
            return Convert.ToSingle(s, CultureInfo.InvariantCulture);
        }
    }
}