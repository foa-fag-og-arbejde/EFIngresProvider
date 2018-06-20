using System;

namespace EFIngresProvider.Helpers
{
    internal static class EntityUtils
    {
        static internal T CheckArgumentNull<T>(T value, string parameterName) where T : class
        {
            if (null == value)
            {
                throw new ArgumentNullException(parameterName);
            }
            return value;
        }
    }
}
