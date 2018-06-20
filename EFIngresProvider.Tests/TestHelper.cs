using EFIngresProvider.Tests.TestModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace EFIngresProvider.Tests
{
    public class TestHelper
    {
        public const string ProviderName = "EFIngresProvider";

        public string ConnectionString => TestConnection.TestEntities;

        public string DirectConnectionString => TestConnection.TestDbDirect;

        public TestEntities CreateTestEntities()
        {
            var context = new TestEntities(DirectConnectionString);
            SetCommandEvents(context);
            return context;
        }

        public DbProviderFactory GetFactoryViaDbProviderFactories()
        {
            return DbProviderFactories.GetFactory(ProviderName);
        }

        public DbProviderServices GetProviderServicesViaDbProviderFactories()
        {
            return GetProviderServicesViaDbProviderFactories(GetFactoryViaDbProviderFactories());
        }

        public DbProviderServices GetProviderServicesViaDbProviderFactories(DbProviderFactory factory)
        {
            IServiceProvider iserviceprovider = factory as IServiceProvider;
            return (DbProviderServices)iserviceprovider.GetService(typeof(DbProviderServices));
        }

        public DbConnection CreateDbConnection()
        {
            var factory = DbProviderFactories.GetFactory(ProviderName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = DirectConnectionString;
            SetCommandEvents(connection);
            return connection;
        }

        public DbConnection OpenDbConnection()
        {
            var connection = CreateDbConnection();
            connection.Open();
            return connection;
        }

        private DbParameter[] GetParameters(DbCommand cmd, IEnumerable<object> parameters)
        {
            return parameters.Select((p, i) =>
            {
                var param = p as DbParameter;
                if (param != null)
                {
                    p = param.Value;
                }
                param = cmd.CreateParameter();
                param.Value = p ?? DBNull.Value;
                param.ParameterName = string.Format("@p{0}", i);
                return param;
            }).ToArray();
        }

        private DbCommand CreateCommand(DbConnection connection, string sql, IEnumerable<object> parameters)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(GetParameters(cmd, parameters));
            return cmd;
        }

        public int ExecuteSql(string sql, params object[] parameters)
        {
            using (var connection = OpenDbConnection())
            using (var cmd = CreateCommand(connection, sql, parameters))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        public void ExecuteSqlIgnoreError(string sql, params object[] parameters)
        {
            try
            {
                ExecuteSql(sql, parameters);
            }
            catch { }
        }

        public object SelectScalar(string sql, params object[] parameters)
        {
            using (var connection = OpenDbConnection())
            using (var cmd = CreateCommand(connection, sql, parameters))
            {
                return cmd.ExecuteScalar();
            }
        }

        public T SelectScalar<T>(string sql, params object[] parameters)
        {
            return (T)SelectScalar(sql, parameters);
        }

        public T Select<T>(Func<TestEntities, T> select)
        {
            using (var db = CreateTestEntities())
            {
                return select(db);
            }
        }

        public void ClearTable(string table)
        {
            ExecuteSqlIgnoreError(string.Format("delete from k_{0}", table));
            ExecuteSqlIgnoreError(string.Format("delete from {0}", table));
            ExecuteSqlIgnoreError(string.Format("delete from k_{0}", table));
        }

        public static void SetCommandEvents(ObjectContext context)
        {
            SetCommandEvents(context.Connection);
        }

        public static void SetCommandEvents(DbContext context)
        {
            SetCommandEvents(context.Database.Connection);
        }

        public static void SetCommandEvents(DbConnection connection)
        {
            if (connection is EntityConnection)
            {
                SetCommandEvents(((EntityConnection)connection).StoreConnection);
            }
            else
            {
                var efIngresConnection = connection as EFIngresConnection;
                if (efIngresConnection != null)
                {
                    efIngresConnection.CommandStarted += new EventHandler<EFIngresCommandEventArgs>(efIngresConnection_CommandStarted);
                    efIngresConnection.CommandModified += new EventHandler<EFIngresCommandEventArgs>(efIngresConnection_CommandModified);
                    efIngresConnection.CommandExecuted += new EventHandler<EFIngresCommandEventArgs>(efIngresConnection_CommandExecuted);
                    efIngresConnection.StateChange += new StateChangeEventHandler(efIngresConnection_StateChange);
                }
            }
        }

        public static bool IsLogging { get; set; }
        public static void Log(string format = "", params object[] args)
        {
            if (IsLogging)
            {
                Console.WriteLine(format, args);
            }
        }

        private static void efIngresConnection_CommandStarted(object sender, EFIngresCommandEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            e.Info = stopwatch;
            Log(new string('-', 100));
            Log("SQL");
            Log("{0}", e.Command.CommandText);
            if (e.Command.Parameters.Count > 0)
            {
                Log();
                Log("  Parameters:");
                foreach (DbParameter param in e.Command.Parameters)
                {
                    Log("    {0} = {1}", param.ParameterName, QuoteValue(param.Value));
                }
            }
            Log();
        }

        private static void efIngresConnection_CommandModified(object sender, EFIngresCommandEventArgs e)
        {
            Log("MODIFIED SQL");
            Log("{0}", e.Command.CommandText);
            if (e.Command.Parameters.Count > 0)
            {
                Log();
                Log("  Parameters:");
                foreach (DbParameter param in e.Command.Parameters)
                {
                    Log("    {0} = {1}", param.ParameterName, QuoteValue(param.Value));
                }
            }
            Log();
        }

        private static string QuoteValue(object value)
        {
            if (value == null)
            {
                return "null";
            }
            if (value is DBNull)
            {
                return "DBNull";
            }
            if (value is string)
            {
                return string.Format(@"""{0}""", value);
            }
            return string.Format(@"{0}", value);
        }

        private static void efIngresConnection_CommandExecuted(object sender, EFIngresCommandEventArgs e)
        {
            var stopwatch = ((Stopwatch)e.Info);
            stopwatch.Stop();
            Log("  Elapsed: {0}", stopwatch.Elapsed);
            Log("  Result: {0}", e.Result);
            if (!e.Success)
            {
                Log();
                Log("  Statement failed!");
                foreach (var line in e.Error.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    Log("    {0}", line);
                }
            }
            Log(new string('-', 100));
        }

        private static void efIngresConnection_StateChange(object sender, StateChangeEventArgs e)
        {
            Log("Connection state changed from {0} to {1}", e.OriginalState, e.CurrentState);
        }
    }
}
