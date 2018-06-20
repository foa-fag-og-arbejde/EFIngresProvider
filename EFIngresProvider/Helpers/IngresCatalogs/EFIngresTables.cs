namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresTables : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresTables", @"
                select Id          = '[' + trim(table_owner) + '][' + trim(table_name) + ']',
                       CatalogName = dbmsinfo('database'),
                       SchemaName  = trim(table_owner),
                       Name        = trim(table_name),
                       table_owner = table_owner,
                       table_name  = table_name,
                       table_type  = table_type,
                       unique_rule = unique_rule
                  from iitables
                 where table_type in ('T', 'V')
                   and table_name not like 'iietab_%'
            ", "structure = isam", "key = (SchemaName, Name)", "fillfactor = 100");
        }
    }
}
