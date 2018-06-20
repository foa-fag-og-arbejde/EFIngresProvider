using System;

namespace EFIngresProvider
{
    public class EFIngresCommandEventArgs : EventArgs
    {
        public EFIngresCommand Command { get; internal set; }
        public bool Success { get; internal set; }
        public Exception Error { get; internal set; }
        public object Result { get; set; }
        public object Info { get; set; }
    }
}
