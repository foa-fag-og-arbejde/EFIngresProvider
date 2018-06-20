namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresFunctions : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresFunctions", @"
                select Id                        = varchar(null),
                       CatalogName               = varchar(null),
                       SchemaName                = varchar(null),
                       Name                      = varchar(null),
                       ReturnTypeName            = varchar(null),
                       ReturnMaxLength           = int4(null),
                       ReturnPrecision           = int4(null),
                       ReturnDateTimePrecision   = int4(null),
                       ReturnScale               = int4(null),
                       ReturnCollationCatalog    = varchar(null),
                       ReturnCollationSchema     = varchar(null),
                       ReturnCollationName       = varchar(null),
                       ReturnCharacterSetCatalog = varchar(null),
                       ReturnCharacterSetSchema  = varchar(null),
                       ReturnCharacterSetName    = varchar(null),
                       ReturnIsMultiSet          = int1(null),
                       IsAggregate               = int1(null),
                       IsBuiltIn                 = int1(null),
                       IsNiladic                 = int1(null)
                  from iiprocedure
                 where 1 = 0
            ");
        }
    }
}
