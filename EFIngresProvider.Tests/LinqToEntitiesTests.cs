using EFIngresProvider.Tests.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class LinqToEntitiesTests : TestBase
    {
        protected override void BeforeTestInitialize()
        {
            TestData.CreateTables();
            TestData.InsertCustomers();
        }

        [TestMethod]
        public void LinqToEntitiesQueryParameterized()
        {
            using (var context = TestHelper.CreateTestEntities())
            {
                var query = from c in context.Customer
                            where c.CustomerID == "ALFKI"
                            select c;

                foreach (Customer c in query)
                {
                    Assert.AreEqual<string>("Alfreds Futterkiste", c.CompanyName);
                }
            }
        }

        [TestMethod]
        public void LinqToEntitiesProviderStoreFunctionQuery()
        {
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.City == "London"
                    select c.CompanyName + " - Company";

                foreach (var name in query)
                {
                    Assert.AreEqual<string>("Around the Horn - Company", name);
                }
            }
        }

        [TestMethod]
        public void LinqToEntitiesQueryWithStartsWith()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.StartsWith("La")
                    select c;

                foreach (var c in query)
                {
                    results.Add(c.CompanyName);
                }
            }
            Assert.AreEqual<int>(4, results.Count);
            Assert.IsTrue(results.Contains("La corne d'abondance"));
            Assert.IsTrue(results.Contains("La maison d'Asie"));
            Assert.IsTrue(results.Contains("Laughing Bacchus Wine Cellars"));
            Assert.IsTrue(results.Contains("Lazy K Kountry Store"));
        }

        [TestMethod]
        public void LinqToEntitiesSkipAndTake()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var skip = 5;
                var take = 5;
                var query =
                    from c in context.Customer
                    orderby c.ContactTitle
                    select c;

                foreach (var c in query.Skip(skip).Take(take))
                {
                    results.Add(c.CompanyName);
                }
            }
            Assert.AreEqual<int>(5, results.Count);
            Assert.IsTrue(results.Contains("Ana Trujillo Emparedados y helados"));
            Assert.IsTrue(results.Contains("La maison d'Asie"));
            Assert.IsTrue(results.Contains("Alfreds Futterkiste"));
            Assert.IsTrue(results.Contains("La corne d'abondance"));
            Assert.IsTrue(results.Contains("Around the Horn"));
        }

        [TestMethod]
        public void LinqToEntitiesSkip()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var skip = 10;
                var query =
                    from c in context.Customer
                    orderby c.ContactTitle
                    select c;

                foreach (var c in query.Skip(skip))
                {
                    results.Add(c.CompanyName);
                }
            }
            Assert.AreEqual<int>(2, results.Count);
            Assert.IsTrue(results.Contains("Blauer See Delikatessen"));
            Assert.IsTrue(results.Contains("Lehmanns Marktstand"));
        }

        [TestMethod]
        public void LinqToEntitiesTake()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var take = 3;
                var query =
                    from c in context.Customer
                    orderby c.ContactTitle
                    select c;

                foreach (var c in query.Take(take))
                {
                    results.Add(c.CompanyName);
                }
            }
            Assert.AreEqual<int>(3, results.Count);
            Assert.IsTrue(results.Contains("Laughing Bacchus Wine Cellars"));
            Assert.IsTrue(results.Contains("Blondesddsl père et fils"));
            Assert.IsTrue(results.Contains("Lazy K Kountry Store"));
        }

        [TestMethod]
        public void LinqToEntitiesLike()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.Like("%Bacchus%Cellars%")
                    select c.CompanyName;

                results.AddRange(query);
            }
            Assert.AreEqual<int>(1, results.Count);
            Assert.IsTrue(results.Contains("Laughing Bacchus Wine Cellars"));
        }

        [TestMethod]
        public void LinqToEntitiesLikeIgnoreCaseTrue()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.Like("%bacchus%cellars%", true)
                    select c.CompanyName;

                results.AddRange(query);
            }
            Assert.AreEqual<int>(1, results.Count);
            Assert.IsTrue(results.Contains("Laughing Bacchus Wine Cellars"));
        }

        [TestMethod]
        public void LinqToEntitiesLikeIgnoreCaseFalse()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.Like("%bacchus%cellars%", false)
                    select c.CompanyName;

                results.AddRange(query);
            }
            Assert.AreEqual<int>(0, results.Count);
        }

        [TestMethod]
        public void LinqToEntitiesLessThan()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.LessThan("B")
                    select c.CompanyName;

                results.AddRange(query);
            }
            Assert.AreEqual<int>(4, results.Count);
        }

        [TestMethod]
        public void LinqToEntitiesLessThanOrEqual()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.LessThanOrEqual("Bl")
                    select c.CompanyName;

                results.AddRange(query);
            }
            Assert.AreEqual<int>(5, results.Count);
        }

        [TestMethod]
        public void LinqToEntitiesGreaterThan()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.GreaterThan("B")
                    select c.CompanyName;

                results.AddRange(query);
            }
            Assert.AreEqual<int>(9, results.Count);
        }

        [TestMethod]
        public void LinqToEntitiesGreaterThanOrEqual()
        {
            var results = new List<string>();
            using (var context = TestHelper.CreateTestEntities())
            {
                var query =
                    from c in context.Customer
                    where c.CompanyName.GreaterThanOrEqual("Bl")
                    select c.CompanyName;

                results.AddRange(query);
            }
            Assert.AreEqual<int>(8, results.Count);
        }
    }
}
