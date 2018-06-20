using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Data.Framework;
using Microsoft.VisualStudio.Data.Framework.AdoDotNet;
using Microsoft.VisualStudio.Data.Services.SupportEntities;
using System.Data.Common;
using EFIngresDDEXProvider.ObjectSelectors;

namespace EFIngresDDEXProvider
{
    /// <summary>
    /// Represents a custom data object selector to supplement or replace
    /// the schema collections supplied by the .NET Framework Data Provider
    /// for SQL Server.  Many of the enumerations here are required for full
    /// support of the built in data design scenarios.
    /// </summary>
    internal class EFIngresObjectSelector : DataObjectSelector
    {
        private static Dictionary<string, ObjectSelector> _selectors = new Dictionary<string, ObjectSelector>(StringComparer.OrdinalIgnoreCase);

        static EFIngresObjectSelector()
        {
            AddSelector(new RootSelector());
            AddSelector(new TableSelector());
            AddSelector(new ColumnSelector());
            AddSelector(new IndexSelector());
            AddSelector(new IndexColumnSelector());
            AddSelector(new ForeignKeySelector());
            AddSelector(new ForeignKeyColumnSelector());
            AddSelector(new ViewSelector());
            AddSelector(new ViewColumnSelector());
            AddSelector(new DatabaseProcedureSelector());
            AddSelector(new DatabaseProcedureParameterSelector());
        }

        private static void AddSelector(ObjectSelector selector)
        {
            _selectors.Add(selector.TypeName, selector);
        }

        private ObjectSelector GetSelector(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }
            ObjectSelector selector;
            if (_selectors.TryGetValue(typeName, out selector))
            {
                return selector;
            }
            throw new NotSupportedException();
        }

        #region Protected Methods

        protected override IList<string> GetRequiredRestrictions(string typeName, object[] parameters)
        {
            return GetSelector(typeName).GetRequiredRestrictions(parameters).ToList();
        }

        protected override IVsDataReader SelectObjects(string typeName, object[] restrictions, string[] properties, object[] parameters)
        {
            var selector = GetSelector(typeName);

            // Execute a SQL statement to get the property values
            //EFIngresConnection connection = Site.GetLockedProviderObject() as EFIngresConnection;
            var connection = Site.GetLockedProviderObject() as DbConnection;
            try
            {
                Debug.Assert(connection != null, "Invalid provider object.");
                if (connection == null)
                {
                    // This should never occur
                    throw new NotSupportedException();
                }

                // Ensure the connection is open
                if (Site.State != DataConnectionState.Open)
                {
                    Site.Open();
                }

                return new AdoDotNetReader(selector.SelectObjects(connection, restrictions));
            }
            finally
            {
                Site.UnlockProviderObject();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This method formats a SQL string by specifying format arguments
        /// based on restrictions.  All enumerations require at least a
        /// database restriction, which is specified twice with different
        /// escape characters.  This is followed by each restriction in turn
        /// with the quote character escaped.  Where there is no restriction,
        /// a default restriction value is added to ensure the SQL statement
        /// is still valid.
        /// </summary>
        private static string FormatSqlString(string sql, object[] restrictions, object[] defaultRestrictions)
        {
            Debug.Assert(sql != null);
            Debug.Assert(restrictions != null);
            Debug.Assert(restrictions.Length > 0);
            Debug.Assert(restrictions[0] is string);
            Debug.Assert(defaultRestrictions != null);
            Debug.Assert(defaultRestrictions.Length >= restrictions.Length);

            object[] formatArgs = new object[defaultRestrictions.Length + 1];
            formatArgs[0] = (restrictions[0] as string).Replace("]", "]]");
            for (int i = 0; i < defaultRestrictions.Length; i++)
            {
                if (restrictions.Length > i && restrictions[i] != null)
                {
                    formatArgs[i + 1] = "N'" + restrictions[i].ToString().Replace("'", "''") + "'";
                }
                else
                {
                    formatArgs[i + 1] = defaultRestrictions[i];
                }
            }
            return String.Format(CultureInfo.CurrentCulture, sql, formatArgs);
        }

        #endregion

        #region Private Constants

        private const string rootEnumerationSql = @"
            SELECT ""Login""    = dbmsinfo('system_user'),
                   ""Database"" = dbmsinfo('database'),
                   ""User""     = dbmsinfo('system_user'),
                   ""Schema""   = dbmsinfo('dba')
        ";

        private const string indexEnumerationSql = @"
            select ""Database""  = dbmsinfo('database'),
                   ""Schema""    = trim(i.base_owner),
                   ""Table""     = trim(i.base_name),
                   ""Name""      = trim(i.index_name),
                   ""IsUnique""  = case i.unique_rule when 'U' then 1 else 0 end,
                   ""IsPrimary"" = case ifnull(c.constraint_type, '') when 'P' then 1 else 0 end
              from iiindexes i
              left join iiconstraint_indexes ci on
                   ci.schema_name   = i.index_owner
               and ci.index_name    = i.index_name
              left join iiconstraints c on
                   c.schema_name     = ci.schema_name
               and c.constraint_name = ci.constraint_name
               and c.text_sequence   = 1
             where i.system_use  = 'U'
               and i.base_owner  = {2}
               and i.base_name   = {3}
               and i.index_name  = {4}
             order by 1, 2, 3, 4
        ";

        private static string[] indexEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)",
            "i.name"
        };

        private const string indexColumnEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [Table] = OBJECT_NAME(o.object_id)," +
            "   [Index] = i.name," +
            "   [Name] = c.name," +
            "   [Ordinal] = ic.key_ordinal" +
            " FROM" +
            "   [{0}].sys.index_columns ic INNER JOIN" +
            "   [{0}].sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id INNER JOIN" +
            "   [{0}].sys.indexes i ON c.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN" +
            "   [{0}].sys.objects o ON i.object_id = o.object_id INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   ic.column_id > 0 AND" +
            "   i.type <> 0 AND" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3} AND" +
            "   i.name = {4} AND" +
            "   c.name = {5}" +
            " ORDER BY" +
            "   1,2,3,4,6";
        private static string[] indexColumnEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)",
            "i.name",
            "c.name"
        };

        private const string foreignKeyEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [Table] = OBJECT_NAME(o.object_id)," +
            "   [Name] = fk.name," +
            "   [ReferencedTableSchema] = SCHEMA_NAME(rk.schema_id)," +
            "   [ReferencedTableName] = OBJECT_NAME(rk.object_id)," +
            "   [UpdateAction] = fk.update_referential_action," +
            "   [DeleteAction] = fk.delete_referential_action" +
            " FROM" +
            "   [{0}].sys.foreign_keys fk INNER JOIN" +
            "   [{0}].sys.objects rk ON fk.referenced_object_id = rk.object_id INNER JOIN" +
            "   [{0}].sys.objects o ON fk.parent_object_id = o.object_id INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3} AND" +
            "   fk.name = {4}" +
            " ORDER BY" +
            "   1,2,3,4";
        private static string[] foreignKeyEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)",
            "fk.name"
        };

        private const string foreignKeyColumnEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [Table] = OBJECT_NAME(o.object_id)," +
            "   [ForeignKey] = fk.name," +
            "   [Name] = fc.name," +
            "   [Ordinal] = fkc.constraint_column_id," +
            "   [ReferencedColumnName] = rc.name" +
            " FROM" +
            "   [{0}].sys.foreign_key_columns fkc INNER JOIN" +
            "   [{0}].sys.columns fc ON fkc.parent_object_id = fc.object_id AND fkc.parent_column_id = fc.column_id INNER JOIN" +
            "   [{0}].sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id INNER JOIN" +
            "   [{0}].sys.foreign_keys fk ON fkc.constraint_object_id = fk.object_id INNER JOIN" +
            "   [{0}].sys.objects rk ON fk.referenced_object_id = rk.object_id INNER JOIN" +
            "   [{0}].sys.objects o ON fk.parent_object_id = o.object_id INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3} AND" +
            "   fk.name = {4} AND" +
            "   fc.name = {5}" +
            " ORDER BY" +
            "   1,2,3,4,6";
        private static string[] foreignKeyColumnEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)",
            "fk.name",
            "fc.name"
        };

        private const string storedProcedureEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [Name] = o.name" +
            " FROM" +
            "   [{0}].sys.objects o INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   o.type IN ('P', 'PC') AND" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3}" +
            " ORDER BY" +
            "   1,2,3";
        private static string[] storedProcedureEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)"
        };

        private const string storedProcedureParameterEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [DatabaseProcedure] = o.name," +
            "   [Name] = p.name," +
            "   [Ordinal] = p.parameter_id," +
            "   [DataType] = t.name," +
            "   [MaxLength] = CASE WHEN t.name IN (N'nchar', N'nvarchar') THEN p.max_length/2 ELSE p.max_length END," +
            "   [Precision] = p.precision," +
            "   [Scale] = p.scale," +
            "   [IsOutput] = p.is_output" +
            " FROM" +
            "   [{0}].sys.parameters p INNER JOIN" +
            "   [{0}].sys.types t ON p.system_type_id = t.user_type_id INNER JOIN" +
            "   [{0}].sys.objects o ON p.object_id = o.object_id INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   o.type IN ('P', 'PC') AND" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3} AND" +
            "   p.name = {4}" +
            " ORDER BY" +
            "   1,2,3,5";
        private static string[] storedProcedureParameterEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)",
            "p.name"
        };

        private const string functionEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [Name] = o.name," +
            "   [Type] = o.type" +
            " FROM" +
            "   [{0}].sys.objects o INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   o.type IN ('AF', 'FN', 'FS', 'FT', 'IF', 'TF') AND" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3}" +
            " ORDER BY" +
            "   1,2,3";
        private static string[] functionEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)"
        };

        private const string functionParameterEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [Function] = o.name," +
            "   [Name] = CASE WHEN p.parameter_id = 0 THEN N'@RETURN_VALUE' ELSE p.name END," +
            "   [Ordinal] = p.parameter_id," +
            "   [DataType] = t.name," +
            "   [MaxLength] = CASE WHEN t.name IN (N'nchar', N'nvarchar') THEN p.max_length/2 ELSE p.max_length END," +
            "   [Precision] = p.precision," +
            "   [Scale] = p.scale," +
            "   [IsOutput] = p.is_output" +
            " FROM" +
            "   [{0}].sys.parameters p INNER JOIN" +
            "   [{0}].sys.types t ON p.system_type_id = t.user_type_id INNER JOIN" +
            "   [{0}].sys.objects o ON p.object_id = o.object_id INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   o.type IN ('AF', 'FN', 'FS', 'FT', 'IF', 'TF') AND" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3} AND" +
            "   p.name = {4}" +
            " ORDER BY" +
            "   1,2,3,5";
        private static string[] functionParameterEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)",
            "p.name"
        };

        private const string functionColumnEnumerationSql =
            "SELECT" +
            "   [Database] = d.name," +
            "   [Schema] = SCHEMA_NAME(o.schema_id)," +
            "   [Function] = o.name," +
            "   [Name] = c.name," +
            "   [Ordinal] = c.column_id," +
            "   [DataType] = t.name," +
            "   [MaxLength] = CASE WHEN t.name IN (N'nchar', N'nvarchar') THEN c.max_length/2 ELSE c.max_length END," +
            "   [Precision] = c.precision," +
            "   [Scale] = c.scale" +
            " FROM" +
            "   [{0}].sys.columns c INNER JOIN" +
            "   [{0}].sys.types t ON c.system_type_id = t.user_type_id INNER JOIN" +
            "   [{0}].sys.objects o ON c.object_id = o.object_id AND o.type IN ('AF', 'FN', 'FS', 'FT', 'IF', 'TF') INNER JOIN" +
            "   master.sys.databases d ON d.name = {1}" +
            " WHERE" +
            "   SCHEMA_NAME(o.schema_id) = {2} AND" +
            "   OBJECT_NAME(o.object_id) = {3} AND" +
            "   c.name = {4}" +
            " ORDER BY" +
            "   1,2,3,5";
        private static string[] functionColumnEnumerationDefaults =
        {
            "d.name",
            "SCHEMA_NAME(o.schema_id)",
            "OBJECT_NAME(o.object_id)",
            "c.name"
        };

        #endregion
    }

}
