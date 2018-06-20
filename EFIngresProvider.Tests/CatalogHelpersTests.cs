using EFIngresProvider.Helpers.IngresCatalogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class CatalogHelpersTests : TestBase
    {
        [TestMethod]
        public void EFIngresConstraintColumns()
        {
            using (var connection = (EFIngresConnection)TestHelper.CreateDbConnection())
            {
                // Arrange
                connection.Open();
                var catalogHelpers = new CatalogHelpers(connection);

                // Act
                catalogHelpers.CreateCatalog("EFIngresConstraintColumns");

                // Assert
            }
        }
    }
}
