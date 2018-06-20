namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class ViewColumnSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.ViewColumn; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { "dbmsinfo('database')", "t.table_owner", "t.table_name", "c.column_name" }; }
        }

        protected override string GetSql()
        {
            return @"
                select ""Database""     = dbmsinfo('database'),
                       ""Schema""       = trim(t.table_owner),
                       ""Table""        = trim(t.table_name),
                       ""Name""         = trim(c.column_name),
                       ""Ordinal""      = c.column_sequence,
                       ""DataType""     = case when c.column_datatype = 'INTEGER' and c.column_length = 1 then 'tinyint'
                                               when c.column_datatype = 'INTEGER' and c.column_length = 2 then 'smallint'
                                               when c.column_datatype = 'INTEGER' and c.column_length = 4 then 'integer'
                                               when c.column_datatype = 'INTEGER' and c.column_length = 8 then 'bigint'
                                               when c.column_datatype = 'INTEGER' then 'integer' + varchar(c.column_length)
                                               when c.column_datatype = 'FLOAT' and c.column_length = 4 then 'float4'
                                               when c.column_datatype = 'FLOAT' and c.column_length = 8 then 'float'
                                               when c.column_datatype = 'FLOAT' then 'float' + varchar(c.column_length)
                                               else lowercase(trim(c.column_datatype)) end,
                       ""MaxLength""    = case when c.column_datatype in ('C', 'CHAR', 'VARCHAR', 'NCHAR', 'NVARCHAR', 'TEXT', 'BYTE', 'BYTE VARYING') then c.column_length 
                                               else null end,
                       ""Precision""    = case when c.column_datatype = 'DECIMAL' then c.column_length
                                               when c.column_datatype = 'INTEGER' and c.column_length = 1 then 3
                                               when c.column_datatype = 'INTEGER' and c.column_length = 2 then 5
                                               when c.column_datatype = 'INTEGER' and c.column_length = 4 then 10
                                               when c.column_datatype = 'INTEGER' and c.column_length = 8 then 19
                                               when c.column_datatype = 'FLOAT'   and c.column_length = 4 then 7
                                               when c.column_datatype = 'FLOAT'   and c.column_length = 8 then 24
                                               when c.column_datatype = 'MONEY'   then 14
                                               else null end,
                       ""Scale""        = case when c.column_datatype in ('INTEGER', 'FLOAT', 'DECIMAL', 'MONEY') then c.column_scale else null end,
                       ""IsNullable""   = case when c.column_nulls = 'Y' then 1 else 0 end,
                       ""DefaultValue"" = c.column_default_val
                  from iicolumns c
                  join iitables t on
                       t.table_owner = c.table_owner
                   and t.table_name  = c.table_name
                   and t.system_use  = 'U'
                   and t.table_type  = 'V'
                   and t.table_owner not in ('ingres', '$ingres')
                   and t.table_owner = {1}
                   and t.table_name  = {2}
                 where 1 = 0
                   and dbmsinfo('database') = {0}
                   and c.column_name        = {3}
                 order by 1, 2, 3, 5
            ";
        }
    }
}
