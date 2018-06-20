using EFIngresProvider.Tests.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class TestDatabaseTests : TestBase
    {
        [TestMethod]
        public void CreateTables()
        {
            // Arrange

            // Act
            TestData.CreateTables();

            // Assert
        }
    }
}
