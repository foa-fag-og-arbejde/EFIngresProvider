using System;
using System.Data.Objects.DataClasses;
using System.Text.RegularExpressions;

namespace EFIngresProvider
{
    public static partial class EFIngresFunctions
    {
        [EdmFunction("Ingres", "CHAR")]
        public static string Char(byte expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "CHAR")]
        public static string Char(sbyte expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "CHAR")]
        public static string Char(short expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "CHAR")]
        public static string Char(ushort expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "CHAR")]
        public static string Char(int expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "CHAR")]
        public static string Char(uint expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "CHAR")]
        public static string Char(long expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "CHAR")]
        public static string Char(ulong expression)
        {
            return expression.ToString();
        }

        [EdmFunction("Ingres", "Like")]
        public static bool Like(this string expression, string pattern)
        {
            return expression.Like(pattern, false);
        }

        [EdmFunction("Ingres", "Like")]
        public static bool Like(this string expression, string pattern, bool ignoreCase)
        {
            var re = GetRegexForLikePattern(pattern, ignoreCase);
            return re.IsMatch(expression);
        }

        [EdmFunction("Ingres", "AddTime")]
        public static DateTime? Add(this DateTime? date, TimeSpan? time)
        {
            return date + time;
        }

        [EdmFunction("Ingres", "AddTime")]
        public static DateTime? Add(this TimeSpan? time, DateTime? date)
        {
            return date + time;
        }

        [EdmFunction("Ingres", "AddTime")]
        public static TimeSpan? Add(this TimeSpan? time1, TimeSpan? time2)
        {
            return time1 + time2;
        }

        [EdmFunction("Ingres", "SubtractTime")]
        public static DateTime? Subtract(this DateTime? date, TimeSpan? time)
        {
            return date - time;
        }

        [EdmFunction("Ingres", "SubtractTime")]
        public static TimeSpan? Subtract(this TimeSpan? time1, TimeSpan? time2)
        {
            return time1 - time2;
        }

        private static Regex GetRegexForLikePattern(string pattern, bool ignoreCase)
        {
            pattern = pattern.Replace("%", ".*");
            pattern = pattern.Replace("_", ".");
            pattern = pattern.Replace(@"\[", "[");
            pattern = pattern.Replace(@"\]", "]");
            pattern = $"^{pattern}$";
            var options = RegexOptions.None;
            if (ignoreCase)
            {
                options = options | RegexOptions.IgnoreCase;
            }
            return new Regex(pattern, options);
        }
    }
}
