namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresProcedures : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresProcedures", @"
                select distinct
                       Id          = '[' + trim(procedure_owner) + '][' + trim(procedure_name) + ']',
                       CatalogName = dbmsinfo('database'),
                       SchemaName  = trim(procedure_owner),
                       Name        = trim(procedure_name)
                  from iiprocedures
            ");
        }
    }
}
