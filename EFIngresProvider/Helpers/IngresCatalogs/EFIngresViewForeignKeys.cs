namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresViewForeignKeys : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresViewForeignKeys", @"
                select Id                  = varchar(null),
                       ToColumnId          = varchar(null),
                       FromColumnId        = varchar(null),
                       ConstraintId        = varchar(null),
                       Ordinal             = int4(null)
                  from iitables
                 where 1 = 0
            ");
        }
    }
}
