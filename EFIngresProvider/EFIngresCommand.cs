using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Ingres.Client;
using System.Data;
using System.Text.RegularExpressions;
using EFIngresProvider.Helpers;

namespace EFIngresProvider
{
    public partial class EFIngresCommand : DbCommand, ICloneable
    {
        #region Initialization

        public EFIngresCommand()
            : this(null, null)
        {
        }

        public EFIngresCommand(string commandText)
            : this(commandText, null)
        {
        }

        public EFIngresCommand(EFIngresConnection connection)
            : this(null, connection)
        {
        }

        public EFIngresCommand(string commandText, EFIngresConnection connection)
        {
            _wrappedCommand = new IngresCommand();
            CommandText = commandText;
            DbConnection = connection;
        }

        #endregion

        #region Properties

        public IngresCommand _wrappedCommand = new IngresCommand();
        public DbCommand WrappedCommand { get { return _wrappedCommand; } }
        public IngresCommand WrappedIngresCommand { get { return _wrappedCommand; } }

        private EFIngresConnection _eFIngresConnection;
        private EFIngresConnection EFIngresConnection
        {
            get { return _eFIngresConnection; }
            set
            {
                _eFIngresConnection = value;
                if (value == null)
                {
                    WrappedCommand.Connection = null;
                    WrappedCommand.Transaction = null;
                }
                else
                {
                    WrappedCommand.Connection = value.WrappedConnection;
                    WrappedCommand.Transaction = value.Transaction;
                }
            }
        }

        protected override DbConnection DbConnection
        {
            get { return EFIngresConnection; }
            set { EFIngresConnection = (EFIngresConnection)value; }
        }

        public override string CommandText
        {
            get { return WrappedCommand.CommandText; }
            set { WrappedCommand.CommandText = value ?? string.Empty; }
        }

        public override int CommandTimeout
        {
            get { return WrappedCommand.CommandTimeout; }
            set { WrappedCommand.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return WrappedCommand.CommandType; }
            set { WrappedCommand.CommandType = value; }
        }

        protected override DbTransaction DbTransaction
        {
            get { return WrappedCommand.Transaction; }
            set { WrappedCommand.Transaction = value; }
        }

        private bool _designTimeVisible = true;
        public override bool DesignTimeVisible
        {
            get { return _designTimeVisible; }
            set { _designTimeVisible = value; }
        }

        public bool Direct { get; set; }

        public string ModifiedCommandText { get; private set; }
        public IEnumerable<IDbDataParameter> ModifiedParameters { get; private set; }

        #endregion

        #region Methods

        public override void Cancel()
        {
            WrappedCommand.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return WrappedCommand.CreateParameter();
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return WrappedCommand.Parameters; }
        }

        protected T Execute<T>(Func<T> execute)
        {
            ModifiedCommandText = null;
            ModifiedParameters = null;

            T result;
            var eventArgs = new EFIngresCommandEventArgs { Command = this, Success = true };
            using (new CultureReestablisher())
            {
                EFIngresConnection.OnCommandStarted(eventArgs);
                try
                {
                    if (Direct)
                    {
                        result = execute();
                        eventArgs.Result = result;
                    }
                    else
                    {
                        using (var parser = new CommandParser(this))
                        {
                            ModifiedCommandText = CommandText;
                            ModifiedParameters = Parameters.Cast<IDbDataParameter>().ToList();
                            EFIngresConnection.OnCommandModified(eventArgs);
                            EFIngresConnection.CatalogHelpers.CreateCatalogs(parser.CatalogTables);
                            result = execute();
                            eventArgs.Result = result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex = new EFIngresCommandException(this, ex.Message, ex);
                    eventArgs.Success = false;
                    eventArgs.Error = ex;
                    throw ex;
                }
                finally
                {
                    EFIngresConnection.OnCommandExecuted(eventArgs);
                }
            }
            return result;
        }

        public override int ExecuteNonQuery()
        {
            if (string.IsNullOrWhiteSpace(CommandText))
            {
                return 1;
            }
            return Execute(() => ExecuteNonQueryInternal());
        }

        public override object ExecuteScalar()
        {
            return Execute(() => ExecuteScalarInternal());
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return Execute(() => ExecuteDbDataReaderInternal(behavior));
        }

        private int ExecuteNonQueryInternal()
        {
            return WrappedCommand.ExecuteNonQuery();
        }

        private object ExecuteScalarInternal()
        {
            var reader = ExecuteDbDataReaderInternal(CommandBehavior.SingleRow);

            Object obj = null;  // default to returning null if no result set

            if (reader.Read())  // read one row; return null if empty
            {
                obj = reader.GetValue(0);  // return first column of first row
                this.Cancel();             // cancel the remainder of the result set
            }

            reader.Close();
            return obj;
        }

        private DbDataReader ExecuteDbDataReaderInternal(CommandBehavior behavior)
        {
            return new EFIngresDataReader(WrappedIngresCommand.ExecuteReader(behavior), behavior)
            {
                UseIngresDate = EFIngresConnection.UseIngresDate,
                TrimChars = EFIngresConnection.TrimChars
            };
        }

        public override void Prepare()
        {
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return WrappedCommand.UpdatedRowSource; }
            set { WrappedCommand.UpdatedRowSource = value; }
        }

        #endregion

        #region CommandParser

        private class CommandParser : IDisposable
        {
            public CommandParser(EFIngresCommand command)
            {
                _command = command;
                _oldCommandText = _command.CommandText;
                _oldParameters = _command.Parameters.Cast<DbParameter>().ToList();
                CatalogTables = new List<string>();
                ChangeParameters();
            }

            private EFIngresCommand _command;
            private string _oldCommandText;
            private IEnumerable<DbParameter> _oldParameters;
            public List<string> CatalogTables { get; private set; }

            private class SqlPart
            {
                public SqlPart(string type, string value)
                    : this(type, value, value)
                {
                }

                public SqlPart(string type, string value, string lexeme)
                {
                    Type = type;
                    Value = value;
                    Lexeme = lexeme;
                }

                public string Type { get; private set; }
                public string Value { get; private set; }
                public string Lexeme { get; private set; }
            }

            private static IEnumerable<SqlPart> ParseSql(string sql)
            {
                var re = new Regex(@"(?<String>'[^']*')|@(?<Parameter>\w+)|(?<Parameter><§£\w+£§>)|EFIngres\.(?<CatalogTable>EFIngres\w+)|""EFIngres""\.""(?<CatalogTable>EFIngres\w+)""", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var groupNames = re.GetGroupNames().Skip(1);

                var start = 0;
                foreach (Match match in re.Matches(sql))
                {
                    if (match.Index > start)
                    {
                        yield return new SqlPart("Sql", sql.Substring(start, match.Index - start));
                    }

                    yield return groupNames.Select(x => new { Name = x, Group = match.Groups[x] })
                                           .Where(x => x.Group.Success)
                                           .Select(x => new SqlPart(x.Name, x.Group.Value, match.Value))
                                           .Single();

                    start = match.Index + match.Value.Length;
                }
                if (start < sql.Length)
                {
                    yield return new SqlPart("Sql", sql.Substring(start));
                }
            }

            private void ChangeParameters()
            {
                var paramMap = _oldParameters.ToDictionary(x => x.ParameterName, x => x);

                _command.Parameters.Clear();
                var paramNo = 0;
                var sql = new StringBuilder();
                foreach (var part in ParseSql(_command.CommandText))
                {
                    if (part.Type == "Parameter")
                    {
                        DbParameter param;
                        if (paramMap.TryGetValue(part.Value, out param) || paramMap.TryGetValue(part.Lexeme, out param))
                        {
                            var addParam = true;
                            var placeholder = "?";

                            param = (DbParameter)((ICloneable)param).Clone();
                            param.ParameterName = string.Format("p{0}", paramNo);
                            if (param.Value is IngresDate || param.Value is DateTime || param.Value is TimeSpan)
                            {
                                var ingresDate = IngresDate.Create(param.Value);
                                if (ingresDate.IngresDateKind == IngresDateKind.DateTime)
                                {
                                    var localValue = ingresDate.Kind == DateTimeKind.Utc ? ingresDate.ToLocalTime() : ingresDate;
                                    if (localValue.IsDate)
                                    {
                                        placeholder = IngresDate.Format(localValue);
                                        addParam = false;
                                    }
                                }
                                else
                                {
                                    placeholder = IngresDate.Format(ingresDate);
                                    addParam = false;
                                }
                            }
                            else if (param.Value is int || param.Value is short || param.Value is long)
                            {
                                placeholder = param.Value.ToString();
                                addParam = false;
                            }

                            if (addParam)
                            {
                                _command.Parameters.Add(param);
                                paramNo += 1;
                            }
                            sql.Append(placeholder);
                        }
                        else
                        {
                            sql.Append(part.Lexeme);
                        }
                    }
                    else if (part.Type == "CatalogTable")
                    {
                        CatalogTables.Add(part.Value);
                        sql.Append("session.");
                        sql.Append(part.Value);
                    }
                    else
                    {
                        sql.Append(part.Lexeme);
                    }
                }
                _command.CommandText = sql.ToString();
            }

            private void RestoreParameters()
            {
                _command.CommandText = _oldCommandText;
                _command.Parameters.Clear();
                foreach (var param in _oldParameters)
                {
                    _command.Parameters.Add(param);
                }
            }

            public void Dispose()
            {
                RestoreParameters();
            }
        }

        #endregion

        #region ICloneable

        object ICloneable.Clone()
        {
            EFIngresCommand clone = new EFIngresCommand();

            clone.EFIngresConnection = EFIngresConnection;
            clone._wrappedCommand = (IngresCommand)((ICloneable)WrappedCommand).Clone();

            return clone;
        }

        #endregion
    }
}
