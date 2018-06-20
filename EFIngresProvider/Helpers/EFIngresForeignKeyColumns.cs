using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using Ingres.Client;
using System.Text.RegularExpressions;

namespace EFIngresProvider.Helpers
{
    public class EFIngresForeignKeyColumns
    {
        public static void CreateForeignKeyColumns(DbConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    DropTable(connection);
                    CreateTable(connection);
                    Populate(connection);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static void DropTable(DbConnection connection)
        {
            try
            {
                ExecuteSql(connection, @"drop table EFIngresForeignKeyColumns");
            }
            catch { /* Do nothing */ }
        }

        private static void CreateTable(DbConnection connection)
        {
            ExecuteSql(connection, @"
                    create table EFIngresForeignKeyColumns (
                        from_constraint_name varchar(32) not null,
                        from_schema_name     varchar(32) not null,
                        from_table_name      varchar(32) not null,
                        from_column_name     varchar(32) not null,
                        to_schema_name       varchar(32) not null,
                        to_table_name        varchar(32) not null,
                        to_column_name       varchar(32) not null
                    )
                ");
            ExecuteSql(connection, @"grant select on EFIngresForeignKeyColumns to public");
        }

        private static void Populate(DbConnection connection)
        {
            var foriegnKeys = new List<EFIngresForeignKeyColumns>();

            var sql = @"
                    select from_schema_name     = trim(r.ref_schema_name),
                           from_table_name      = trim(r.ref_table_name),
                           from_constraint_name = trim(r.ref_constraint_name),
                           to_schema_name       = trim(r.unique_schema_name),
                           to_table_name        = trim(r.unique_table_name),
                           text_sequence,
                           text_segment
                      from iiref_constraints r
                      join iiconstraints c on
                           c.schema_name     = r.ref_schema_name
                       and c.table_name      = r.ref_table_name
                       and c.constraint_name = r.ref_constraint_name
                     order by r.ref_schema_name, r.ref_table_name, r.ref_constraint_name, c.text_sequence
                ";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                EFIngresForeignKeyColumns foreignKey = null;
                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        var text_sequence = (long)reader["text_sequence"];
                        if (text_sequence == 1)
                        {
                            foreignKey = new EFIngresForeignKeyColumns
                            {
                                FromSchemaName = (string)reader["from_schema_name"],
                                FromTableName = (string)reader["from_table_name"],
                                FromConstraintName = (string)reader["from_constraint_name"],
                                ToSchemaName = (string)reader["to_schema_name"],
                                ToTableName = (string)reader["to_table_name"]
                            };
                            foriegnKeys.Add(foreignKey);
                        }
                        foreignKey.AddText((string)reader["text_segment"]);
                    }
                }
            }
            foriegnKeys.ForEach(x => x.Insert(connection));
        }

        private static int ExecuteSql(DbConnection connection, string sql, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        private EFIngresForeignKeyColumns() { }

        public string FromSchemaName { get; set; }
        public string FromTableName { get; set; }
        public string FromConstraintName { get; set; }
        public string ToSchemaName { get; set; }
        public string ToTableName { get; set; }
        public string Text { get { return _textBuilder.ToString(); } }
        public List<ForeignKeyColumn> Columns { get; set; }

        private StringBuilder _textBuilder = new StringBuilder();

        public void Insert(DbConnection connection)
        {
            Parse();
            foreach (var column in Columns)
            {
                ExecuteSql(connection, @"
                            insert into EFIngresForeignKeyColumns (
                                from_constraint_name,
                                from_schema_name,
                                from_table_name,
                                from_column_name,
                                to_schema_name,
                                to_table_name,
                                to_column_name
                            ) values (
                                @from_constraint_name,
                                @from_schema_name,
                                @from_table_name,
                                @from_column_name,
                                @to_schema_name,
                                @to_table_name,
                                @to_column_name
                            )
                        ",
                    new IngresParameter("@from_constraint_name", FromConstraintName),
                    new IngresParameter("@from_schema_name", FromSchemaName),
                    new IngresParameter("@from_table_name", FromTableName),
                    new IngresParameter("@from_column_name", column.FromColumnName),
                    new IngresParameter("@to_schema_name", ToSchemaName),
                    new IngresParameter("@to_table_name", ToTableName),
                    new IngresParameter("@to_column_name", column.ToColumnName)
                );
            }
        }

        public void AddText(string text)
        {
            _textBuilder.Append(text);
        }

        private static Regex _foreignKeyRe = new Regex(@"^\s*FOREIGN\s+KEY\s*\((.+)\)\s*REFERENCES.*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public void Parse()
        {
            var match = _foreignKeyRe.Match(Text);
            if (match.Success)
            {
                var fromColumns = ParseColumns(match.Groups[1].Value);
                var toColumns = ParseColumns(match.Groups[2].Value);
                Columns = new List<ForeignKeyColumn>();
                for (var i = 0; i < fromColumns.Count; i++)
                {
                    Columns.Add(new ForeignKeyColumn
                    {
                        Ordinal = i + 1,
                        FromColumnName = fromColumns[i],
                        ToColumnName = toColumns[i]
                    });
                }
            }
        }

        private static List<string> ParseColumns(string match)
        {
            return Regex.Split(match, @",")
                        .Select(x => x.Trim())
                        .Select(x => Regex.Replace(x, @"^""(.*)""$", @"$1"))
                        .ToList();
        }

        public class ForeignKeyColumn
        {
            public int Ordinal { get; set; }
            public string FromColumnName { get; set; }
            public string ToColumnName { get; set; }

            public override string ToString()
            {
                return string.Format(@"{0}: {1} = {2}", Ordinal, FromColumnName, ToColumnName);
            }
        }
    }
}
