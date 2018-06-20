using System.Linq;
using System.Data.Common;
using EFIngresProvider.Helpers.IngresCatalogs;

namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class ForeignKeySelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.ForeignKey; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { }; }
        }

        public override DbDataReader SelectObjects(DbConnection connection, object[] restrictions)
        {
            var foreignKeys = ForeignKey.GetForeignKeys(connection).Select(x => new {
                Database = x.DatabaseName,
                Schema = x.SchemaName,
                Table = x.TableName,
                Name = x.ConstraintName,
                ReferencedTableSchema = x.ToSchemaName,
                ReferencedTableName = x.ToTableName,
                UpdateAction = x.UpdateRule,
                DeleteAction = x.DeleteRule
            });

            object restriction;
            if (GetRestriction(1, restrictions, out restriction))
            {
                foreignKeys = foreignKeys.Where(x => x.Schema == restriction.ToString());
            }
            if (GetRestriction(2, restrictions, out restriction))
            {
                foreignKeys = foreignKeys.Where(x => x.Table == restriction.ToString());
            }
            if (GetRestriction(3, restrictions, out restriction))
            {
                foreignKeys = foreignKeys.Where(x => x.Name == restriction.ToString());
            }
            if (GetRestriction(4, restrictions, out restriction))
            {
                foreignKeys = foreignKeys.Where(x => x.ReferencedTableSchema == restriction.ToString());
            }
            if (GetRestriction(5, restrictions, out restriction))
            {
                foreignKeys = foreignKeys.Where(x => x.ReferencedTableName == restriction.ToString());
            }
            if (GetRestriction(6, restrictions, out restriction))
            {
                foreignKeys = foreignKeys.Where(x => x.UpdateAction == restriction.ToString());
            }
            if (GetRestriction(6, restrictions, out restriction))
            {
                foreignKeys = foreignKeys.Where(x => x.DeleteAction == restriction.ToString());
            }
            return ObjectReader.GreateReader(foreignKeys);
        }
    }
}
