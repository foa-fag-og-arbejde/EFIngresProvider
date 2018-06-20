using System;

namespace EFIngresProvider
{
    public class EFIngresException : Exception
    {
        public EFIngresException() : base() { }
        public EFIngresException(string message) : base(message) { }
        public EFIngresException(string message, Exception innerException) : base(message, innerException) { }
    }
}
