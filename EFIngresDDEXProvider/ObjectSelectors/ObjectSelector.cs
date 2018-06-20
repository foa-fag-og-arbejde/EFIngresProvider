using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Diagnostics;

namespace EFIngresDDEXProvider.ObjectSelectors
{
    public abstract class ObjectSelector
    {
        public abstract string TypeName { get; }

        public virtual IEnumerable<string> GetRequiredRestrictions(object[] parameters)
        {
            return Enumerable.Empty<string>();
            // yield return "Database";
        }

        public virtual DbDataReader SelectObjects(DbConnection connection, object[] restrictions)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = FormatSql(GetSql(), restrictions);
                return cmd.ExecuteReader();
            }
        }

        protected virtual string[] DefaultRestrictions
        {
            get { return new string[] { "dbmsinfo('database')" }; }
        }

        protected virtual string GetSql()
        {
            return string.Empty;
        }

        protected virtual string FormatSql(string sql, object[] restrictions)
        {
            Debug.Assert(sql != null);
            Debug.Assert(DefaultRestrictions != null);
            Debug.Assert((restrictions == null) || (DefaultRestrictions.Length >= restrictions.Length));

            if (DefaultRestrictions.Length > 0)
            {
                var args = Enumerable.Range(0, DefaultRestrictions.Length)
                                     .Select(i => GetSqlRestriction(i, restrictions))
                                     .ToArray();

                return String.Format(sql, args);
            }
            else
            {
                return sql;
            }
        }

        protected bool GetRestriction(int i, object[] restrictions, out object restriction)
        {
            if ((restrictions != null) && (restrictions.Length > i) && (restrictions[i] != null))
            {
                restriction = restrictions[i];
            }
            else
            {
                restriction = DefaultRestrictions[i];
            }
            return restriction != null;
        }

        protected string GetSqlRestriction(int i, object[] restrictions)
        {
            if ((restrictions != null) && (restrictions.Length > i) && (restrictions[i] != null))
            {
                return "'" + restrictions[i].ToString().Replace("'", "''") + "'";
            }
            return DefaultRestrictions[i];
        }
    }
}
