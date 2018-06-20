using System.Linq;
using System.Data.Common;
using EFIngresProvider.Helpers.IngresCatalogs;

namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class ForeignKeyColumnSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.ForeignKeyColumn; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { }; }
        }

        public override DbDataReader SelectObjects(DbConnection connection, object[] restrictions)
        {
            var fkColumns = ForeignKey.GetForeignKeys(connection)
                                      .SelectMany(x => x.Columns.Select(column => new
                                      {
                                          Database = x.DatabaseName,
                                          Schema = x.SchemaName,
                                          Table = x.TableName,
                                          ForeignKey = x.ConstraintName,
                                          Name = column.FromColumnName,
                                          Ordinal = column.Ordinal,
                                          ReferencedColumnName = column.ToColumnName
                                      }));
            return ObjectReader.GreateReader(fkColumns);
        }
    }
}
