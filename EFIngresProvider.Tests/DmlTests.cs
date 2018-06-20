using EFIngresProvider.Tests.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class DmlTests : TestBase
    {
        private int SelectRowCount(string customerId, string companyName)
        {
            return TestHelper.SelectScalar<int>(string.Format("SELECT COUNT(*) FROM Customers WHERE CustomerID = '{0}' AND CompanyName = '{1}'", customerId, companyName));
        }

        private Customer SelectCustomer(string customerId)
        {
            using (var context = TestHelper.CreateTestEntities())
            {
                return context.Customer.Find(customerId);
            }
        }

        private Customer InsertCustomer(Customer customer)
        {
            using (var context = TestHelper.CreateTestEntities())
            {
                context.Customer.Add(new Customer(customer));
                context.SaveChanges();
            }
            return SelectCustomer(customer.CustomerID);
        }

        protected override void BeforeTestInitialize()
        {
            TestData.CreateTables();
        }

        [TestMethod]
        public void InsertCustomer()
        {
            // Arrange
            var expected = new Customer
            {
                CustomerID = "TESTI",
                CompanyName = "A Test Customer - Insert"
            };

            // Act
            using (var context = TestHelper.CreateTestEntities())
            {
                context.Customer.Add(new Customer(expected));
                context.SaveChanges();
            }
            var actual = SelectCustomer(expected.CustomerID);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpdateCustomer()
        {
            // Arrange
            var customer = InsertCustomer(new Customer
            {
                CustomerID = "TESTU",
                CompanyName = "A Test Customer - Update"
            });

            var expected = new Customer(customer)
            {
                CompanyName = "New Company Name"
            };

            // Act
            using (var context = TestHelper.CreateTestEntities())
            {
                var customerToUpdate = context.Customer.Find(customer.CustomerID);
                customerToUpdate.CompanyName = expected.CompanyName;
                context.SaveChanges();
            }
            var actual = SelectCustomer(customer.CustomerID);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeleteCustomer()
        {
            // Arrange
            var customer = InsertCustomer(new Customer
            {
                CustomerID = "TESTD",
                CompanyName = "A Test Customer - Update"
            });

            // Act
            using (var context = TestHelper.CreateTestEntities())
            {
                var customerToDelete = context.Customer.Find(customer.CustomerID);
                context.Customer.Remove(customerToDelete);
                context.SaveChanges();
            }

            var actual = SelectCustomer(customer.CustomerID);

            // Assert
            Assert.IsNull(actual);
        }
    }
}
