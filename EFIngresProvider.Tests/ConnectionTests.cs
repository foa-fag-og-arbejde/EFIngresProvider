using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class ConnectionTests : TestBase
    {
        [TestMethod]
        public void GetProviderViaDbProviderFactories()
        {
            // Arrange

            // Act
            var factory = DbProviderFactories.GetFactory(TestHelper.ProviderName);

            // Assert
            Assert.IsNotNull(factory, string.Format("DbProviderFactories.GetFactory({0})!", TestHelper.ProviderName));
            Assert.IsInstanceOfType(factory, typeof(EFIngresProviderFactory), string.Format("DbProviderFactories.GetFactory({0})!", TestHelper.ProviderName));
        }

        [TestMethod]
        public void GetConnectionViaDbProviderFactory()
        {
            // Arrange
            var factory = DbProviderFactories.GetFactory(TestHelper.ProviderName);

            // Act
            using (var connection = factory.CreateConnection())
            {
                // Assert
                Assert.IsNotNull(connection, "factory.CreateConnection()");
                Assert.IsInstanceOfType(connection, typeof(DbConnection), "factory.CreateConnection()");
            }
        }

        [TestMethod]
        public void OpenConnectionViaDbProviderFactory()
        {
            // Arrange
            var factory = DbProviderFactories.GetFactory(TestHelper.ProviderName);

            using (var connection = factory.CreateConnection())
            {
                // Act
                connection.ConnectionString = TestHelper.DirectConnectionString;
                connection.Open();

                // Assert
            }
        }
    }
}
