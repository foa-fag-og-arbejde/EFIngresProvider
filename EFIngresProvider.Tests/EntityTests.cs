using EFIngresProvider.Tests.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class EntityTests : TestBase
    {
        private void ClearTables()
        {
            TestHelper.ClearTable("ansaettelse");
        }

        protected override void AfterTestInitialize()
        {
            base.AfterTestInitialize();
            ClearTables();
        }

        [TestMethod]
        public void UpdateAnsaettelse()
        {
            // Arrange
            using (var db = TestHelper.CreateTestEntities())
            {
                db.ansaettelse.Add(new ansaettelse
                {
                    medl_ident = 1,
                    lbnr = 8,
                    arbst_nr = 6,
                    fra_dato = new DateTime(1996, 8, 25),
                    til_dato = IngresDate.EmptyDateTimeValue,
                    form = "9",
                    arbejds_time = 0.10m,
                    primaer_ansaettelse = "j",
                    reg_tid = new DateTime(1997, 9, 3, 23, 0, 0),
                    reg_init = "J7K7L7M7N7O7",
                    reg_vers_nr = 9
                });
                db.SaveChanges();
            }

            // Act
            using (var db = TestHelper.CreateTestEntities())
            {
                var ansaettelse = db.ansaettelse.Single(a => a.medl_ident == 1 && a.lbnr == 8);
                ansaettelse.arbejds_time = 1.1m;
                ansaettelse.reg_tid = new DateTime(635673775917762767, DateTimeKind.Local);
                ansaettelse.reg_init = "mlu";
                ansaettelse.reg_vers_nr = ansaettelse.reg_vers_nr + 1;
                db.SaveChanges();
            }
            var actual = TestHelper.Select(db => db.ansaettelse.Single(a => a.medl_ident == 1 && a.lbnr == 8));

            // Assert
            Assert.IsNotNull(actual);
            Assert.AreEqual(1.1m, actual.arbejds_time);
            Assert.AreEqual(new DateTime(2015, 5, 16, 12, 53, 11), actual.reg_tid);
            Assert.AreEqual("mlu", actual.reg_init);
            Assert.AreEqual(10, actual.reg_vers_nr);
        }
    }
}
