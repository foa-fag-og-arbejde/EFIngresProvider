using System;
using System.Data;

namespace EFIngresProvider
{
    public class EFIngresConnectionEventArgs : EventArgs
    {
        internal EFIngresConnectionEventArgs(EFIngresConnection connection)
        {
            Connection = connection;
            OriginalState = connection.State;
            CurrentState = connection.State;
        }

        internal EFIngresConnectionEventArgs StateChanged(StateChangeEventArgs e)
        {
            OriginalState = e.OriginalState;
            CurrentState = e.CurrentState;
            return this;
        }

        public EFIngresConnection Connection { get; private set; }
        public ConnectionState CurrentState { get; private set; }
        public ConnectionState OriginalState { get; private set; }
        public object Info { get; set; }
    }
}
