using System.Linq;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class EFIngresForeignKeys : CatalogHelper
    {
        protected override void CreateCatalogInternal()
        {
            if (!SessionTableExists("EFIngresForeignKeys"))
            {
                DropAndCreateSessionTable("EFIngresForeignKeys",
                    "Id           varchar(2000) not null",
                    "Ordinal      integer       not null",
                    "ConstraintId varchar(2000) not null",
                    "FromColumnId varchar(2000) not null",
                    "ToColumnId   varchar(2000) not null"
                );

                var fkColumns = ForeignKey.GetForeignKeys(Connection)
                                          .SelectMany(x => x.Columns.Select(column => new
                                          {
                                              Id = GetId(x.SchemaName, x.TableName, x.ConstraintName, column.Ordinal),
                                              ConstraintId = GetId(x.SchemaName, x.TableName, x.ConstraintName),
                                              FromColumnId = GetId(x.SchemaName, x.TableName, column.FromColumnName),
                                              ToColumnId = GetId(x.ToSchemaName, x.ToTableName, column.ToColumnName),
                                              Ordinal = column.Ordinal
                                          })).ToList();
                PopulateSessionTable("EFIngresForeignKeys", fkColumns);
            }
        }
    }
}
