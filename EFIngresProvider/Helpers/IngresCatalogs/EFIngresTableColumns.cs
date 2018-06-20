using System.Collections.Generic;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresTableColumns : CatalogHelper
    {
        protected override IEnumerable<string> DependsOn
        {
            get
            {
                yield return "EFIngresTables";
            }
        }

        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresTableColumns", @"
                select Id                  = '[' + trim(c.table_owner) + '][' + trim(c.table_name) + '][' + trim(c.column_name) + ']',
                       ParentId            = '[' + trim(c.table_owner) + '][' + trim(c.table_name) + ']',
                       Name                = trim(c.column_name),
                       Ordinal             = c.column_sequence,
                       IsNullable          = case when c.column_nulls = 'Y' then 1 else 0 end,
                       TypeName            = case when c.column_datatype = 'INTEGER' and c.column_length = 1 then 'tinyint'
                                                  when c.column_datatype = 'INTEGER' and c.column_length = 2 then 'smallint'
                                                  when c.column_datatype = 'INTEGER' and c.column_length = 4 then 'integer'
                                                  when c.column_datatype = 'INTEGER' and c.column_length = 8 then 'bigint'
                                                  when c.column_datatype = 'INTEGER' then 'integer' + varchar(c.column_length)
                                                  when c.column_datatype = 'FLOAT' and c.column_length = 4 then 'float4'
                                                  when c.column_datatype = 'FLOAT' and c.column_length = 8 then 'float'
                                                  when c.column_datatype = 'FLOAT' then 'float' + varchar(c.column_length)
                                                  else lowercase(trim(c.column_datatype)) end,
                       MaxLength           = case when c.column_datatype in ('C', 'CHAR', 'VARCHAR', 'NCHAR', 'NVARCHAR', 'TEXT', 'BYTE', 'BYTE VARYING') then c.column_length
                                                  else null end,
                       Precision           = case when c.column_datatype = 'DECIMAL' then c.column_length
                                                  when c.column_datatype = 'INTEGER' and c.column_length = 1 then 3
                                                  when c.column_datatype = 'INTEGER' and c.column_length = 2 then 5
                                                  when c.column_datatype = 'INTEGER' and c.column_length = 4 then 10
                                                  when c.column_datatype = 'INTEGER' and c.column_length = 8 then 19
                                                  when c.column_datatype = 'FLOAT'   and c.column_length = 4 then 7
                                                  when c.column_datatype = 'FLOAT'   and c.column_length = 8 then 24
                                                  when c.column_datatype = 'MONEY'   then 14
                                                  else null end,
                       DateTimePrecision   = case when c.column_datatype in ('INGRESDATE', 'ANSIDATE')
                                                  or c.column_datatype like 'TIME%'
                                                  or c.column_datatype like 'INTERVAL%' then c.column_scale
                                                  else null end,
                       Scale               = case when c.column_datatype in ('INTEGER', 'FLOAT', 'DECIMAL', 'MONEY') then c.column_scale else null end,
                       CollationCatalog    = varchar(null),
                       CollationSchema     = varchar(null),
                       CollationName       = varchar(null),
                       CharacterSetCatalog = varchar(null),
                       CharacterSetSchema  = varchar(null),
                       CharacterSetName    = case when c.column_datatype in ('NCHAR', 'NVARCHAR', 'LONG NVARCHAR') then 'UNICODE' else null end,
                       IsMultiSet          = smallint(0),
                       IsIdentity          = case when c.column_default_val like 'next value for %' then int1(1) else int1(0) end,
                       IsStoreGenerated    = case when c.column_system_maintained = 'Y' then int1(1) else int1(0) end,
                       Default             = c.column_default_val,
                       table_owner         = t.table_owner,
                       table_name          = t.table_name,
                       table_type          = t.table_type,
                       column_name         = c.column_name,
                       column_sequence     = c.column_sequence
                  from session.EFIngresTables t
                  join iicolumns c on
                       c.table_owner = t.table_owner
                   and c.table_name  = t.table_name
            ");
        }
    }
}
