namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresViewConstraints : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresViewConstraints", @"
                select Id                  = varchar(null),
                       ParentId            = varchar(null),
                       Name                = varchar(null),
                       ConstraintType      = varchar(null),
                       IsDeferrable        = tinyint(0),
                       IsInitiallyDeferred = tinyint(0),
                       Expression          = varchar(null),
                       UpdateRule          = varchar(null),
                       DeleteRule          = varchar(null)
                  from iitables
                 where 1 = 0
            ");
        }
    }
}
