using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data.Common;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class ForeignKey : Constraint
    {
        public static IEnumerable<ForeignKey> GetForeignKeys(DbConnection connection)
        {
            return GetConstraints<ForeignKey>(connection, reader => new ForeignKey(reader), @"
                select database_name   = dbmsinfo('database'),
                       schema_name     = trim(c.schema_name),
                       table_name      = trim(c.table_name),
                       constraint_name = trim(c.constraint_name),
                       constraint_type = trim(c.constraint_type),
                       to_schema_name  = trim(rc.unique_schema_name),
                       to_table_name   = trim(rc.unique_table_name),
                       update_rule     = case i.consupdrule when 0 then 'NO ACTION'
                                                            when 1 then 'RESTRICT'
                                                            when 2 then 'CASCADE'
                                                            when 3 then 'SET NULL'
                                                            else varchar(consupdrule) end,
                       delete_rule     = case i.consdelrule when 0 then 'NO ACTION'
                                                            when 1 then 'RESTRICT'
                                                            when 2 then 'CASCADE'
                                                            when 3 then 'SET NULL'
                                                            else varchar(consdelrule) end,
                       text_sequence   = c.text_sequence,
                       text_segment    = c.text_segment
                  from iiconstraints c
                  join iirelation r on
                       r.relowner = c.schema_name
                   and r.relid    = c.table_name
                  join iiintegrity i on
                       i.inttabbase = r.reltid
                   and i.inttabidx  = r.reltidx
                   and i.consname   = c.constraint_name
                  join iiref_constraints rc on
                       rc.ref_schema_name     = c.schema_name
                   and rc.ref_table_name      = c.table_name
                   and rc.ref_constraint_name = c.constraint_name
                 where c.system_use != 'S'
                 order by c.schema_name, c.table_name, c.constraint_name, c.text_sequence
            ");
        }

        protected ForeignKey(DbDataReader reader)
            : base(reader)
        {
            ToSchemaName = (string)reader["to_schema_name"];
            ToTableName = (string)reader["to_table_name"];
            UpdateRule = (string)reader["update_rule"];
            DeleteRule = (string)reader["delete_rule"];
        }

        public string ToSchemaName { get; protected set; }
        public string ToTableName { get; protected set; }
        public string UpdateRule { get; protected set; }
        public string DeleteRule { get; protected set; }
        public IEnumerable<ForeignKeyColumn> Columns { get; private set; }

        protected override void SetText(string text)
        {
            base.SetText(text);
            Columns = ParseForeignKeyColumns();

        }

        private static Regex _foreignKeyRe = new Regex(@"^\s*FOREIGN\s+KEY\s*\((.+)\)\s*REFERENCES.*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private IEnumerable<ForeignKeyColumn> ParseForeignKeyColumns()
        {
            var match = _foreignKeyRe.Match(Text);
            if (match.Success)
            {
                var fromColumns = ParseColumns(match.Groups[1].Value);
                var toColumns = ParseColumns(match.Groups[2].Value);
                for (var i = 0; i < fromColumns.Count; i++)
                {
                    yield return new ForeignKeyColumn
                    {
                        Constraint = this,
                        Ordinal = i + 1,
                        FromColumnName = fromColumns[i],
                        ToColumnName = toColumns[i]
                    };
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
            public ForeignKey Constraint { get; set; }
            public int Ordinal { get; set; }
            public string FromColumnName { get; set; }
            public string ToColumnName { get; set; }
        }
    }
}
