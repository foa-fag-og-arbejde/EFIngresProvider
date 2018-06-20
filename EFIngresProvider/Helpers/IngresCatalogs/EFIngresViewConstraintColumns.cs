namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresViewConstraintColumns : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresViewConstraintColumns", @"
                select ConstraintId        = varchar(null),
                       ColumnId            = varchar(null)
                  from iitables
                 where 1 = 0
            ");
        }
    }
}
