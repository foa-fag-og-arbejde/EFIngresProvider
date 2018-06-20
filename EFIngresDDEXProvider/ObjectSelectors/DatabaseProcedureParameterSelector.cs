namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class DatabaseProcedureParameterSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.DatabaseProcedureParameter; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { "dbmsinfo('database')", "p.procedure_owner", "p.procedure_name", "pp.param_name" }; }
        }

        protected override string GetSql()
        {
            return @"
                select ""Database""          = dbmsinfo('database'),
                       ""Schema""            = trim(pp.procedure_owner),
                       ""DatabaseProcedure"" = trim(pp.procedure_name),
                       ""Name""              = trim(pp.param_name),
                       ""Ordinal""           = pp.param_sequence,
                       ""DataType""          = case when pp.param_datatype = 'INTEGER' and pp.param_length = 1 then 'tinyint'
                                                    when pp.param_datatype = 'INTEGER' and pp.param_length = 2 then 'smallint'
                                                    when pp.param_datatype = 'INTEGER' and pp.param_length = 4 then 'integer'
                                                    when pp.param_datatype = 'INTEGER' and pp.param_length = 8 then 'bigint'
                                                    when pp.param_datatype = 'INTEGER' then 'integer' + varchar(pp.param_length)
                                                    when pp.param_datatype = 'FLOAT' and pp.param_length = 4 then 'float4'
                                                    when pp.param_datatype = 'FLOAT' and pp.param_length = 8 then 'float'
                                                    when pp.param_datatype = 'FLOAT' then 'float' + varchar(pp.param_length)
                                                    else lowercase(trim(pp.param_datatype)) end,
                       ""MaxLength""         = case when pp.param_datatype in ('C', 'CHAR', 'VARCHAR', 'NCHAR', 'NVARCHAR', 'TEXT', 'BYTE', 'BYTE VARYING') then pp.param_length 
                                                    else null end,
                       ""Precision""         = case when pp.param_datatype = 'DECIMAL' then pp.param_length
                                                    when pp.param_datatype = 'INTEGER' and pp.param_length = 1 then 3
                                                    when pp.param_datatype = 'INTEGER' and pp.param_length = 2 then 5
                                                    when pp.param_datatype = 'INTEGER' and pp.param_length = 4 then 10
                                                    when pp.param_datatype = 'INTEGER' and pp.param_length = 8 then 19
                                                    when pp.param_datatype = 'FLOAT'   and pp.param_length = 4 then 7
                                                    when pp.param_datatype = 'FLOAT'   and pp.param_length = 8 then 24
                                                    when pp.param_datatype = 'MONEY'   then 14
                                                    else null end,
                       ""Scale""             = case when pp.param_datatype in ('INTEGER', 'FLOAT', 'DECIMAL', 'MONEY') then pp.param_scale else null end,
                       ""IsOutput""          = pp.param_output
                  from iiproc_params pp
                 where dbmsinfo('database') = {0}
                   and pp.param_name        = {3}
                   and exists ( select 1
                                  from iiprocedures p
                                 where p.procedure_owner = pp.procedure_owner
                                   and p.procedure_name  = pp.procedure_name
                                   and p.system_use      = 'U'
                                   and p.procedure_owner not in ('ingres', '$ingres')
                                   and p.procedure_owner    = {1}
                                   and p.procedure_name     = {2} )
                 order by 1, 2, 3, 5
            ";
        }
    }
}
