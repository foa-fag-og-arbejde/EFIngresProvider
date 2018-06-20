using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Data.Common;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public abstract class CatalogHelper
    {
        public static CatalogHelper Create<T>(CatalogHelpers catalogHelpers) where T : CatalogHelper, new()
        {
            return new T { CatalogHelpers = catalogHelpers };
        }

        protected CatalogHelper()
        {
            Created = false;
        }

        protected CatalogHelpers CatalogHelpers { get; private set; }
        protected EFIngresConnection Connection { get { return CatalogHelpers.Connection; } }
        public virtual string Name { get { return this.GetType().Name; } }
        public bool Created { get; private set; }
        protected virtual IEnumerable<string> DependsOn { get { return Enumerable.Empty<string>(); } }

        public void CreateCatalog()
        {
            if (!Created)
            {
                foreach (var tablename in DependsOn)
                {
                    CatalogHelpers.CreateCatalog(tablename);
                }
                CreateCatalogInternal();
                Created = true;
            }
        }

        protected abstract void CreateCatalogInternal();

        #region Protected helper methods

        protected string GetId(params object[] parts)
        {
            return string.Concat(parts.Select(x => string.Format(CultureInfo.InvariantCulture, "[{0}]", x)));
        }

        protected void PopulateSessionTable<T>(string tablename, IEnumerable<T> objs)
        {
            string sql;
            IEnumerable<Dictionary<string, object>> rows;
            using (var reader = ObjectReader.GreateReader(objs))
            {
                sql = string.Format(@"insert into session.{0} ({1}) values ({2})",
                    tablename,
                    string.Join(", ", reader.FieldNames),
                    string.Join(", ", reader.FieldNames.Select(x => "@" + x))
                    );
                rows = reader.Select(r => reader.FieldNames.ToDictionary(x => "@" + x, x => r[x]));
            }
            foreach (var row in rows)
            {
                ExecuteSql(sql, row);
            }
        }

        protected bool SessionTableExists(string tablename)
        {
            try
            {
                var exists = ExecuteScalar<int>(string.Format(@"select table_exists = int4(ifnull(max(1), 1)) from session.{0} where 1 = 0", tablename));
                if (exists == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        protected void DropSessionTable(string tablename)
        {
            try { ExecuteSql(string.Format(@"drop table session.{0}", tablename)); }
            catch { }
        }

        protected void DropAndCreateSessionTable(string tablename, params string[] columns)
        {
            if (!SessionTableExists(tablename))
            {
                DropSessionTable(tablename);
                ExecuteSql(string.Format(@"
                    declare global temporary table session.{0} (
                        {1}
                    )
                    on commit preserve rows
                    with norecovery
                ", tablename, string.Join("," + Environment.NewLine + "                    ", columns)));
            }
        }

        protected void DropAndCreateSessionTableAs(string tablename, string query, params string[] withClauses)
        {
            var with = new List<string>();
            with.Add("norecovery");
            with.AddRange(withClauses);
            if (!SessionTableExists(tablename))
            {
                DropSessionTable(tablename);
                ExecuteSql(string.Format(@"
                    declare global temporary table session.{0} as
                    {1}
                    on commit preserve rows
                    with {2}
                ", tablename, query, string.Join(", ", with)));
            }
        }

        protected T ExecuteScalar<T>(string sql, Dictionary<string, object> parameters = null)
        {
            using (var cmd = CreateCommand(sql, parameters))
            {
                return (T)cmd.ExecuteScalar();
            }
        }

        protected int ExecuteSql(string sql, Dictionary<string, object> parameters = null)
        {
            using (var cmd = CreateCommand(sql, parameters))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        protected DbCommand CreateCommand(string sql, Dictionary<string, object> parameters = null)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = sql;
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters.Select(x => CreateParameter(cmd, x.Key, x.Value)).ToArray());
            }
            return cmd;
        }

        protected DbParameter CreateParameter(DbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            return param;
        }

        #endregion
    }
}
