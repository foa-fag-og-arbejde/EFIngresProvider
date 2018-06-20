using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class CompareFunctionsTests
    {
        [DataTestMethod]
        [DataRow(null, null, false, DisplayName = "null < null")]
        [DataRow(null, "01", false, DisplayName = "null < '01'")]
        [DataRow(null, "02", false, DisplayName = "null < '02'")]
        [DataRow(null, "03", false, DisplayName = "null < '03'")]
        [DataRow("01", null, false, DisplayName = "'01' < null")]
        [DataRow("01", "01", false, DisplayName = "'01' < '01'")]
        [DataRow("01", "02", true, DisplayName = "'01' < '02'")]
        [DataRow("01", "03", true, DisplayName = "'01' < '03'")]
        [DataRow("02", null, false, DisplayName = "'02' < null")]
        [DataRow("02", "01", false, DisplayName = "'02' < '01'")]
        [DataRow("02", "02", false, DisplayName = "'02' < '02'")]
        [DataRow("02", "03", true, DisplayName = "'02' < '03'")]
        [DataRow("03", null, false, DisplayName = "'03' < null")]
        [DataRow("03", "01", false, DisplayName = "'03' < '01'")]
        [DataRow("03", "02", false, DisplayName = "'03' < '02'")]
        [DataRow("03", "03", false, DisplayName = "'03' < '03'")]
        public void StringLessThan(string a, string b, bool expected)
        {
            Assert.AreEqual(expected, a.LessThan(b));
        }

        [DataTestMethod]
        [DataRow(null, null, false, DisplayName = "null <= null")]
        [DataRow(null, "01", false, DisplayName = "null <= '01'")]
        [DataRow(null, "02", false, DisplayName = "null <= '02'")]
        [DataRow(null, "03", false, DisplayName = "null <= '03'")]
        [DataRow("01", null, false, DisplayName = "'01' <= null")]
        [DataRow("01", "01", true, DisplayName = "'01' <= '01'")]
        [DataRow("01", "02", true, DisplayName = "'01' <= '02'")]
        [DataRow("01", "03", true, DisplayName = "'01' <= '03'")]
        [DataRow("02", null, false, DisplayName = "'02' <= null")]
        [DataRow("02", "01", false, DisplayName = "'02' <= '01'")]
        [DataRow("02", "02", true, DisplayName = "'02' <= '02'")]
        [DataRow("02", "03", true, DisplayName = "'02' <= '03'")]
        [DataRow("03", null, false, DisplayName = "'03' <= null")]
        [DataRow("03", "01", false, DisplayName = "'03' <= '01'")]
        [DataRow("03", "02", false, DisplayName = "'03' <= '02'")]
        [DataRow("03", "03", true, DisplayName = "'03' <= '03'")]
        public void StringLessThanOrEqual(string a, string b, bool expected)
        {
            Assert.AreEqual(expected, a.LessThanOrEqual(b));
        }

        [DataTestMethod]
        [DataRow(null, null, false, DisplayName = "null > null")]
        [DataRow(null, "01", false, DisplayName = "null > '01'")]
        [DataRow(null, "02", false, DisplayName = "null > '02'")]
        [DataRow(null, "03", false, DisplayName = "null > '03'")]
        [DataRow("01", null, false, DisplayName = "'01' > null")]
        [DataRow("01", "01", false, DisplayName = "'01' > '01'")]
        [DataRow("01", "02", false, DisplayName = "'01' > '02'")]
        [DataRow("01", "03", false, DisplayName = "'01' > '03'")]
        [DataRow("02", null, false, DisplayName = "'02' > null")]
        [DataRow("02", "01", true, DisplayName = "'02' > '01'")]
        [DataRow("02", "02", false, DisplayName = "'02' > '02'")]
        [DataRow("02", "03", false, DisplayName = "'02' > '03'")]
        [DataRow("03", null, false, DisplayName = "'03' > null")]
        [DataRow("03", "01", true, DisplayName = "'03' > '01'")]
        [DataRow("03", "02", true, DisplayName = "'03' > '02'")]
        [DataRow("03", "03", false, DisplayName = "'03' > '03'")]
        public void StringGreaterThan(string a, string b, bool expected)
        {
            Assert.AreEqual(expected, a.GreaterThan(b));
        }

        [DataTestMethod]
        [DataRow(null, null, false, DisplayName = "null >= null")]
        [DataRow(null, "01", false, DisplayName = "null >= '01'")]
        [DataRow(null, "02", false, DisplayName = "null >= '02'")]
        [DataRow(null, "03", false, DisplayName = "null >= '03'")]
        [DataRow("01", null, false, DisplayName = "'01' >= null")]
        [DataRow("01", "01", true, DisplayName = "'01' >= '01'")]
        [DataRow("01", "02", false, DisplayName = "'01' >= '02'")]
        [DataRow("01", "03", false, DisplayName = "'01' >= '03'")]
        [DataRow("02", null, false, DisplayName = "'02' >= null")]
        [DataRow("02", "01", true, DisplayName = "'02' >= '01'")]
        [DataRow("02", "02", true, DisplayName = "'02' >= '02'")]
        [DataRow("02", "03", false, DisplayName = "'02' >= '03'")]
        [DataRow("03", null, false, DisplayName = "'03' >= null")]
        [DataRow("03", "01", true, DisplayName = "'03' >= '01'")]
        [DataRow("03", "02", true, DisplayName = "'03' >= '02'")]
        [DataRow("03", "03", true, DisplayName = "'03' >= '03'")]
        public void StringGreaterThanOrEqual(string a, string b, bool expected)
        {
            Assert.AreEqual(expected, a.GreaterThanOrEqual(b));
        }
    }
}
