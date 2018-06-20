using EFIngresProvider.Tests.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.Entity.Infrastructure;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class ErrorTests : TestBase
    {
        protected override void BeforeTestInitialize()
        {
            TestData.CreateTables();
        }

        [TestMethod]
        public void TestSqlError()
        {
            // Arrange
            var expected = new ArgumentNullException("path");

            // Act
            var actual = Try(() =>
            {
                using (var context = TestHelper.CreateTestEntities())
                {
                    context.Database.ExecuteSqlCommand(@"drop table ErrorTest");
                    context.ErrorTest.Add(new ErrorTest { ID = "test", Name = "test" });
                    context.SaveChanges();
                }
            });

            // Assert
            Assert.IsInstanceOfType(actual, typeof(DbUpdateException));
            Assert.IsInstanceOfType(actual.InnerException, typeof(UpdateException));
            Assert.IsInstanceOfType(actual.InnerException.InnerException, typeof(EFIngresCommandException));
        }
    }
}
