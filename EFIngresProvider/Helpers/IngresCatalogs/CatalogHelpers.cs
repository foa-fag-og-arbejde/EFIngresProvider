using System;
using System.Collections.Generic;
using System.Linq;

namespace EFIngresProvider.Helpers.IngresCatalogs
{
    public class CatalogHelpers
    {
        public static IEnumerable<string> Catalogs
        {
            get
            {
                var helpers = new CatalogHelpers(null);
                foreach (var helper in helpers.GetHelpers())
                {
                    yield return helper.Name;
                }
            }
        }

        public CatalogHelpers(EFIngresConnection connection)
        {
            Connection = connection;
        }

        public EFIngresConnection Connection { get; private set; }

        private IEnumerable<CatalogHelper> GetHelpers()
        {
            yield return CatalogHelper.Create<EFIngresTables>(this);
            yield return CatalogHelper.Create<EFIngresTableColumns>(this);
            yield return CatalogHelper.Create<EFIngresViews>(this);
            yield return CatalogHelper.Create<EFIngresViewColumns>(this);
            yield return CatalogHelper.Create<EFIngresConstraints>(this);
            yield return CatalogHelper.Create<EFIngresCheckConstraints>(this);
            yield return CatalogHelper.Create<EFIngresConstraintColumns>(this);
            yield return CatalogHelper.Create<EFIngresForeignKeyConstraints>(this);
            yield return CatalogHelper.Create<EFIngresForeignKeys>(this);
            yield return CatalogHelper.Create<EFIngresFunctions>(this);
            yield return CatalogHelper.Create<EFIngresFunctionParameters>(this);
            yield return CatalogHelper.Create<EFIngresProcedures>(this);
            yield return CatalogHelper.Create<EFIngresProcedureParameters>(this);
            yield return CatalogHelper.Create<EFIngresViewConstraints>(this);
            yield return CatalogHelper.Create<EFIngresViewConstraintColumns>(this);
            yield return CatalogHelper.Create<EFIngresViewForeignKeys>(this);
        }

        private Dictionary<string, CatalogHelper> _helpersByName;
        private Dictionary<string, CatalogHelper> HelpersByName
        {
            get
            {
                if (_helpersByName == null)
                {
                    _helpersByName = GetHelpers().ToDictionary(x => x.Name, x => x, StringComparer.InvariantCultureIgnoreCase);
                }
                return _helpersByName;
            }
        }

        public void CreateCatalogs(IEnumerable<string> tablenames)
        {
            foreach (var tablename in tablenames)
            {
                CreateCatalog(tablename);
            }
        }

        public void CreateCatalog(string tablename)
        {
            CatalogHelper helper;
            if (HelpersByName.TryGetValue(tablename, out helper))
            {
                helper.CreateCatalog();
            }
        }
    }
}
