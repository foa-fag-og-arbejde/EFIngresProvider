using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class Constraint
    {
        public static IEnumerable<Constraint> GetConstraints(DbConnection connection)
        {
            return GetConstraints<Constraint>(connection, reader => new Constraint(reader), @"
                select database_name   = dbmsinfo('database'),
                       schema_name     = trim(c.schema_name),
                       table_name      = trim(c.table_name),
                       constraint_name = trim(c.constraint_name),
                       constraint_type = trim(c.constraint_type),
                       text_sequence   = c.text_sequence,
                       text_segment    = c.text_segment
                  from iiconstraints c
                 where c.system_use != 'S'
                 order by c.schema_name, c.table_name, c.constraint_name, c.text_sequence
            ").ToList();
        }

        protected static IEnumerable<ConstraintType> GetConstraints<ConstraintType>(DbConnection connection, Func<DbDataReader, ConstraintType> createConstraint, string sql) where ConstraintType : Constraint
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;

                ConstraintType constraint = null;
                var constraintText = new StringBuilder();
                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        var text_sequence = (long)reader["text_sequence"];
                        if (text_sequence == 1)
                        {
                            if (constraint != null)
                            {
                                constraint.SetText(constraintText.ToString());
                                yield return constraint;
                            }
                            constraint = createConstraint(reader);
                            constraintText = new StringBuilder();
                        }
                        constraintText.Append((string)reader["text_segment"]);
                    }
                }
                if (constraint != null)
                {
                    constraint.SetText(constraintText.ToString());
                    yield return constraint;
                }
            }
        }

        protected Constraint(DbDataReader reader)
        {
            DatabaseName = (string)reader["database_name"];
            SchemaName = (string)reader["schema_name"];
            TableName = (string)reader["table_name"];
            ConstraintName = (string)reader["constraint_name"];
            ConstraintType = (string)reader["constraint_type"];
        }

        public string DatabaseName { get; protected set; }
        public string SchemaName { get; protected set; }
        public string TableName { get; protected set; }
        public string ConstraintName { get; protected set; }
        public string ConstraintType { get; protected set; }
        public string Text { get; protected set; }

        protected virtual void SetText(string text)
        {
            Text = text;
        }
    }
}
