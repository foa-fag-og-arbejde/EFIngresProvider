namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresFunctionParameters : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            DropAndCreateSessionTableAs("EFIngresFunctionParameters", @"
                select Id                  = varchar(null),
                       ParentId            = varchar(null),
                       Name                = varchar(null),
                       Ordinal             = int4(null),
                       TypeName            = varchar(null),
                       MaxLength           = int4(null),
                       Precision           = int4(null),
                       DateTimePrecision   = int4(null),
                       Scale               = int4(null),
                       CollationCatalog    = varchar(null),
                       CollationSchema     = varchar(null),
                       CollationName       = varchar(null),
                       CharacterSetCatalog = varchar(null),
                       CharacterSetSchema  = varchar(null),
                       CharacterSetName    = varchar(null),
                       IsMultiSet          = int1(null),
                       Mode                = varchar(null),
                       Default             = varchar(null)
                  from iiprocedure
                 where 1 = 0
            ");
        }
    }
}
