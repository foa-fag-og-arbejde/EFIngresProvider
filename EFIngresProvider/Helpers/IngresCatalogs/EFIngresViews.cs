namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresViews : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresViews", @"
                select Id          = '[' + trim(table_owner) + '][' + trim(table_name) + ']',
                       CatalogName = dbmsinfo('database'),
                       SchemaName  = trim(table_owner),
                       Name        = trim(table_name)
                  from iitables
                 where 1 = 0
                   and table_type = 'V'
            ");
        }
    }
}
