using System.Linq;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresCheckConstraints : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            if (!SessionTableExists("EFIngresCheckConstraints"))
            {
                DropAndCreateSessionTable("EFIngresCheckConstraints",
                    "Id         varchar(2000)      not null",
                    "Expression varchar(4000)      not null"
                );

                var constraints = Constraint.GetConstraints(Connection)
                                            .Select(x => new
                                            {
                                                Id = GetId(x.SchemaName, x.TableName, x.ConstraintName),
                                                Expression = x.Text
                                            }).ToList();
                PopulateSessionTable("EFIngresCheckConstraints", constraints);
            }
        }
    }
}
