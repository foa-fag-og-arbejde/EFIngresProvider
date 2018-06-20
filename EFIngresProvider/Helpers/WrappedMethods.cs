using Ingres.Client;
using System;
using System.Reflection;

namespace EFIngresProvider.Helpers
{
    internal static class WrappedMethods
    {
        private static MethodInfo IngresConnectionStringBuilderTryGetOrdinalMethod;
        internal static int TryGetOrdinal(this IngresConnectionStringBuilder ingresConnectionStringBuilder, string keyword)
        {
            IngresConnectionStringBuilderTryGetOrdinalMethod = IngresConnectionStringBuilderTryGetOrdinalMethod ?? typeof(IngresConnectionStringBuilder).GetWrappedMethod("TryGetOrdinal", typeof(string));
            return (int)IngresConnectionStringBuilderTryGetOrdinalMethod.Invoke(ingresConnectionStringBuilder, new object[] { keyword });
        }

        private static MethodInfo SqlDataIsNullMethod = null;
        internal static bool SqlDataIsNull(this object data)
        {
            SqlDataIsNullMethod = SqlDataIsNullMethod ?? data.GetType().GetWrappedMethod("isNull");
            return (bool)SqlDataIsNullMethod.Invoke(data, new object[] { });
        }

        private static MethodInfo SqlDataGetStringMethod = null;
        internal static string SqlDataGetString(this object data)
        {
            SqlDataGetStringMethod = SqlDataGetStringMethod ?? data.GetType().GetWrappedMethod("getString");
            return (string)SqlDataGetStringMethod.Invoke(data, new object[] { });
        }

        private static MethodInfo IngresDateGetTimestampMethod = null;
        internal static DateTime SqlDataGetTimestamp(this object data, TimeZone timeZone)
        {
            IngresDateGetTimestampMethod = IngresDateGetTimestampMethod ?? data.GetType().GetWrappedMethod("getTimestamp", typeof(TimeZone));
            return (DateTime)IngresDateGetTimestampMethod.Invoke(data, new object[] { timeZone });
        }

        private static MethodInfo AdvanRsltColumnDataValueMethod = null;
        internal static object AdvanRsltColumnDataValue(this object resultset, int ordinal)
        {
            AdvanRsltColumnDataValueMethod = AdvanRsltColumnDataValueMethod ?? resultset.GetType().GetWrappedMethod("columnDataValue", typeof(int));
            return AdvanRsltColumnDataValueMethod.Invoke(resultset, new object[] { ordinal });
        }
    }
}
