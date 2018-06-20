namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresProcedureParameters : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresProcedureParameters", @"
                select Id                  = '[' + trim(pp.procedure_owner) + '][' + trim(pp.procedure_name) + '][' + trim(pp.param_name) + ']',
                       ParentId            = '[' + trim(pp.procedure_owner) + '][' + trim(pp.procedure_name) + ']',
                       Name                = trim(pp.param_name),
                       Ordinal             = pp.param_sequence,
                       TypeName            = case when pp.param_datatype = 'INTEGER' and pp.param_length = 1 then 'tinyint'
                                                  when pp.param_datatype = 'INTEGER' and pp.param_length = 2 then 'smallint'
                                                  when pp.param_datatype = 'INTEGER' and pp.param_length = 4 then 'integer'
                                                  when pp.param_datatype = 'INTEGER' and pp.param_length = 8 then 'bigint'
                                                  when pp.param_datatype = 'INTEGER' then 'integer' + varchar(pp.param_length)
                                                  when pp.param_datatype = 'FLOAT' and pp.param_length = 4 then 'float4'
                                                  when pp.param_datatype = 'FLOAT' and pp.param_length = 8 then 'float'
                                                  when pp.param_datatype = 'FLOAT' then 'float' + varchar(pp.param_length)
                                                  else lowercase(trim(pp.param_datatype)) end,
                       MaxLength           = case when pp.param_datatype in ('C', 'CHAR', 'VARCHAR', 'NCHAR', 'NVARCHAR', 'TEXT', 'BYTE', 'BYTE VARYING') then pp.param_length 
                                                  else null end,
                       Precision           = case when pp.param_datatype = 'DECIMAL' then pp.param_length
                                                  when pp.param_datatype = 'INTEGER' and pp.param_length = 1 then 3
                                                  when pp.param_datatype = 'INTEGER' and pp.param_length = 2 then 5
                                                  when pp.param_datatype = 'INTEGER' and pp.param_length = 4 then 10
                                                  when pp.param_datatype = 'INTEGER' and pp.param_length = 8 then 19
                                                  when pp.param_datatype = 'FLOAT'   and pp.param_length = 4 then 7
                                                  when pp.param_datatype = 'FLOAT'   and pp.param_length = 8 then 24
                                                  when pp.param_datatype = 'MONEY'   then 14
                                                  else null end,
                       DateTimePrecision   = case when pp.param_datatype in ('INGRESDATE', 'ANSIDATE')
                                                    or pp.param_datatype like 'TIME%'
                                                    or pp.param_datatype like 'INTERVAL%' then pp.param_scale
                                                  else null end,
                       Scale               = case when pp.param_datatype in ('INTEGER', 'FLOAT', 'DECIMAL', 'MONEY') then pp.param_scale else null end,
                       CollationCatalog    = varchar(null),
                       CollationSchema     = varchar(null),
                       CollationName       = varchar(null),
                       CharacterSetCatalog = varchar(null),
                       CharacterSetSchema  = varchar(null),
                       CharacterSetName    = case when pp.param_datatype in ('NCHAR', 'NVARCHAR', 'LONG NVARCHAR') then 'UNICODE' else null end,
                       IsMultiSet          = smallint(0),
                       Mode                = case when pp.param_inout  = 'Y' then 'INOUT'
                                                  when pp.param_input  = 'Y' then 'IN'
                                                  when pp.param_output = 'Y' then 'OUT'
                                                  else null end,
                       Default             = pp.param_default_val
                  from iiproc_params pp
                 where exists ( select 1       
                                  from iiprocedures p
                                 where p.procedure_owner = pp.procedure_owner
                                   and p.procedure_name  = pp.procedure_name )
            ");
        }
    }
}
