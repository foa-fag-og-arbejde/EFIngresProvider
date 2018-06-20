using EFIngresProvider.Tests.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data.Common;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class SimpleSqlTests : TestBase
    {
        protected override void BeforeTestInitialize()
        {
            TestData.CreateTables();
            TestData.InsertCustomers();
        }

        [TestMethod]
        public void GetQueryResultsViaReader()
        {
            var results = new List<string>();

            using (var connection = TestHelper.CreateDbConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT CompanyName FROM Customers WHERE CustomerID LIKE @CustomerID";

                        DbParameter parameter = command.CreateParameter();
                        parameter.ParameterName = "@CustomerID";
                        parameter.Value = "A%";
                        command.Parameters.Add(parameter);

                        try
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    results.Add(reader.GetString(0));
                                }
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }

            Assert.AreEqual<int>(4, results.Count);
            Assert.IsTrue(results.Contains("Alfreds Futterkiste"));
            Assert.IsTrue(results.Contains("Ana Trujillo Emparedados y helados"));
            Assert.IsTrue(results.Contains("Antonio Moreno Taquería"));
            Assert.IsTrue(results.Contains("Around the Horn"));
        }
    }
}
