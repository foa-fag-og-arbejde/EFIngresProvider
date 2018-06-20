using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class EFIngresConnectionStringBuilderTests : TestBase
    {
        [TestMethod]
        public void TestEmptyConnectionString()
        {
            // Arrange

            // Act
            var builder = new EFIngresConnectionStringBuilder();

            // Assert
            Assert.AreEqual("", builder.ConnectionString);
        }

        [TestMethod]
        public void TestSinglePropertyConnectionString()
        {
            // Arrange

            // Act
            var builder = new EFIngresConnectionStringBuilder(@"Use Ingres Date = true");

            // Assert
            Assert.AreEqual("UseIngresDate=True", builder.ConnectionString);
        }

        [TestMethod]
        public void TestSetConnectionString()
        {
            // Arrange
            var builder = new EFIngresConnectionStringBuilder();

            // Act
            builder.ConnectionString = @"Use Ingres Date = true";

            // Assert
            Assert.AreEqual("UseIngresDate=True", builder.ConnectionString);
        }
    }
}
