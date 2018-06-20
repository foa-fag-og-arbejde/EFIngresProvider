using System.Data.Objects.DataClasses;

namespace EFIngresProvider
{
    partial class EFIngresFunctions
    {
        [EdmFunction("Ingres", "LessThan")]
        public static bool LessThan(this string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }
            return left.CompareTo(right) < 0;
        }

        [EdmFunction("Ingres", "LessThanOrEqual")]
        public static bool LessThanOrEqual(this string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }
            return left.CompareTo(right) <= 0;
        }

        [EdmFunction("Ingres", "GreaterThan")]
        public static bool GreaterThan(this string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }
            return left.CompareTo(right) > 0;
        }

        [EdmFunction("Ingres", "GreaterThanOrEqual")]
        public static bool GreaterThanOrEqual(this string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }
            return left.CompareTo(right) >= 0;
        }
    }
}
