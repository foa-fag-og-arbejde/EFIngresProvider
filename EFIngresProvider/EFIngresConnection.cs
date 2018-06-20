using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using Ingres.Client;
using EFIngresProvider.Helpers;
using EFIngresProvider.Helpers.IngresCatalogs;

namespace EFIngresProvider
{
    public partial class EFIngresConnection : DbConnection, ICloneable
    {
        #region Constructors

        private static int _nextConnectionNo = 1;
        private int _connectionNo = _nextConnectionNo++;
        private EFIngresConnectionEventArgs _connectionEventArgs;
        private EFIngresConnectionEventArgs ConnectionEventArgs
        {
            get
            {
                if (_connectionEventArgs == null)
                {
                    _connectionEventArgs = new EFIngresConnectionEventArgs(this);
                }
                return _connectionEventArgs;
            }
        }

        public EFIngresConnection()
        {
            WrappedConnection = new IngresConnection();
            WrappedConnection.StateChange += new StateChangeEventHandler(WrappedConnection_StateChange);
            ConnectionStringBuilder = new EFIngresConnectionStringBuilder();
        }

        public EFIngresConnection(string connectionString)
            : this()
        {
            ConnectionString = connectionString;
        }

        #endregion

        #region events

        public event EventHandler<EFIngresConnectionEventArgs> ConnectionEvent;
        public event EventHandler<EFIngresCommandEventArgs> CommandStarted;
        public event EventHandler<EFIngresCommandEventArgs> CommandModified;
        public event EventHandler<EFIngresCommandEventArgs> CommandExecuted;

        internal void OnCommandStarted(EFIngresCommandEventArgs e)
        {
            OnEvent(CommandStarted, e);
        }

        internal void OnCommandModified(EFIngresCommandEventArgs e)
        {
            OnEvent(CommandModified, e);
        }

        internal void OnCommandExecuted(EFIngresCommandEventArgs e)
        {
            OnEvent(CommandExecuted, e);
        }

        private void OnEvent<EventArgsType>(EventHandler<EventArgsType> handler, EventArgsType e) where EventArgsType : EventArgs
        {
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Event handlers

        void WrappedConnection_StateChange(object sender, StateChangeEventArgs e)
        {
            //WriteLog("EFIngresConnection {0} state changed from {1} to {2}", _connectionNo, e.OriginalState, e.CurrentState);
            OnStateChange(e);
            OnEvent(ConnectionEvent, ConnectionEventArgs.StateChanged(e));
        }

        #endregion

        #region Properties

        internal IngresConnection WrappedConnection { get; private set; }
        internal EFIngresConnectionStringBuilder ConnectionStringBuilder { get; private set; }

        public bool UseIngresDate { get { return ConnectionStringBuilder.UseIngresDate; } }
        public bool TrimChars { get { return ConnectionStringBuilder.TrimChars; } }
        public bool JoinOPGreedy
        {
            get { return ConnectionStringBuilder.JoinOPGreedy; }
            set { ConnectionStringBuilder.JoinOPGreedy = value; }
        }
        public int JoinOPTimeout
        {
            get { return ConnectionStringBuilder.JoinOPTimeout; }
            set { ConnectionStringBuilder.JoinOPTimeout = value; }
        }

        private CatalogHelpers _catalogHelpers = null;
        internal CatalogHelpers CatalogHelpers
        {
            get
            {
                if (_catalogHelpers == null)
                {
                    _catalogHelpers = new CatalogHelpers(this);
                }
                return _catalogHelpers;
            }
        }

        protected override DbProviderFactory DbProviderFactory
        {
            get { return EFIngresProviderFactory.Instance; }
        }

        public override string ServerVersion
        {
            get { return WrappedConnection.ServerVersion; }
        }

        public override ConnectionState State
        {
            get { return WrappedConnection.State; }
        }

        public override string ConnectionString
        {
            get { return ConnectionStringBuilder.ConnectionString; }
            set
            {
                ConnectionStringBuilder.ConnectionString = value;
                //ConnectionStringBuilder.DateFormat = "ISO4";
                ConnectionStringBuilder.DecimalChar = ".";
                ConnectionStringBuilder.SendIngresDates = true;
                WrappedConnection.ConnectionString = ConnectionStringBuilder.IngresConnectionString;
            }
        }

        public override int ConnectionTimeout
        {
            get { return WrappedConnection.ConnectionTimeout; }
        }

        public override string Database
        {
            get { return WrappedConnection.Database; }
        }

        public override string DataSource
        {
            get { return WrappedConnection.DataSource; }
        }

        public override ISite Site
        {
            get { return WrappedConnection.Site; }
            set { WrappedConnection.Site = value; }
        }

        #endregion

        #region Methods

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return WrappedConnection.BeginTransaction(isolationLevel);
        }

        public override void ChangeDatabase(string databaseName)
        {
            _catalogHelpers = null;
            WrappedConnection.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            if ((Transaction != null) && !Transaction.GetWrappedProperty<bool>("HasAlreadyBeenCommittedOrRolledBack") && (Transaction.Connection == null))
            {
                Transaction.SetWrappedField("_connection", WrappedConnection);
            }
            _catalogHelpers = null;
            WrappedConnection.Close();
        }

        public IngresTransaction Transaction
        {
            get { return WrappedConnection.GetWrappedProperty<IngresTransaction>("Transaction"); }
        }

        protected override DbCommand CreateDbCommand()
        {
            DbCommand command = EFIngresProviderFactory.Instance.CreateCommand();
            command.Connection = this;
            return command;
        }

        public EFIngresCommand CreateEFIngresCommand(bool direct = false)
        {
            var cmd = new EFIngresCommand(this);
            cmd.Direct = direct;
            return cmd;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WrappedConnection.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void EnlistTransaction(System.Transactions.Transaction transaction)
        {
            WrappedConnection.EnlistTransaction(transaction);
        }

        public override DataTable GetSchema(string collectionName)
        {
            return WrappedConnection.GetSchema(collectionName);
        }

        public override DataTable GetSchema()
        {
            return WrappedConnection.GetSchema();
        }

        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return WrappedConnection.GetSchema(collectionName, restrictionValues);
        }

        public override void Open()
        {
            _catalogHelpers = null;
            WrappedConnection.Open();
            ExecSql("SET LOCKMODE session WHERE readlock=nolock");
            if (JoinOPGreedy)
            {
                ExecSql("SET JOINOP GREEDY");
            }
            if (JoinOPTimeout > 0)
            {
                ExecSql("SET JOINOP TIMEOUT " + JoinOPTimeout.ToString());
            }
        }

        public int ExecSql(string sql)
        {
            using (var cmd = CreateEFIngresCommand(true))
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region ICloneable

        object ICloneable.Clone()
        {
            throw new NotImplementedException();
        }

        #endregion

        internal void ClearPool()
        {
            throw new NotImplementedException();
        }
    }
}
