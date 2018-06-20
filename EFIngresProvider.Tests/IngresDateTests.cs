using EFIngresProvider.Tests.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class IngresDateTests : TestBase
    {
        protected override void BeforeTestInitialize()
        {
            TestData.CreateTables();
            TestData.InsertIngresDates();
        }

        private class IngresDateTest
        {
            public IngresDateTest(string name, object value, string formattedValue)
            {
                Name = name;
                Value = value;
                FormattedValue = formattedValue;
                FieldType = value == null ? typeof(object) : value.GetType();
                Actual = FormatIngresDate();
            }

            public string Name { get; private set; }
            public object Value { get; private set; }
            public string FormattedValue { get; private set; }
            public Type FieldType { get; private set; }
            public string Actual { get; private set; }

            public string FormatIngresDate()
            {
                if (Value == null)
                {
                    return "";
                }
                if (Value.GetType() == typeof(DateTime))
                {
                    return IngresDate.Format((DateTime)Value);
                }
                if (Value.GetType() == typeof(TimeSpan))
                {
                    return IngresDate.Format((TimeSpan)Value);
                }
                if (Value.GetType() == typeof(IngresDate))
                {
                    return IngresDate.Format((IngresDate)Value);
                }
                return "";
            }

            public override string ToString()
            {
                return string.Format("{0}: {1} ({2} - {3})", Name, Value, Value.GetType().Name);
            }
        }

        [TestMethod]
        public void TestReadDates()
        {
            var results = new List<IngresDateTest>();
            DataTable schema;

            using (var connection = TestHelper.CreateDbConnection())
            {
                connection.Open();
                var slam = connection.GetSchema("DataTypes");
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Name, Value, FormattedValue FROM IngresDateTest";
                    using (var reader = (EFIngresDataReader)command.ExecuteReader())
                    {
                        //reader.UseIngresDate = true;
                        schema = reader.GetSchemaTable();
                        while (reader.Read())
                        {
                            results.Add(new IngresDateTest(reader.GetString(0), reader.GetValue(1), reader.GetString(2)));
                        }
                    }
                }
            }
        }

        private DateTime? GetTestIngresDateTest2(string value)
        {
            TestHelper.ExecuteSql("delete from IngresDateTest2");
            TestHelper.ExecuteSql(string.Format("insert into IngresDateTest2 (Value) values ({0})", value));
            var actual = TestHelper.SelectScalar("select Value from IngresDateTest2");
            if (actual == DBNull.Value)
            {
                return null;
            }
            return (DateTime?)TestHelper.SelectScalar("select Value from IngresDateTest2");
        }

        private DateTime? GetTestIngresDateTest2(DateTime? value)
        {
            TestHelper.ExecuteSql("delete from IngresDateTest2");
            TestHelper.ExecuteSql("insert into IngresDateTest2 (Value) values (@p0)", value);
            var actual = TestHelper.SelectScalar("select Value from IngresDateTest2");
            if (actual == DBNull.Value)
            {
                return null;
            }
            return (DateTime?)TestHelper.SelectScalar("select Value from IngresDateTest2");
        }

        private void TestIngresDateTest2(string value, object expected)
        {
            var actual = GetTestIngresDateTest2(value);
            Assert.AreEqual(expected, actual);
        }

        private void TestIngresDateTest2(string value, DateTime expected)
        {
            var actual = IngresDate.ToDateTime(GetTestIngresDateTest2(value));
            Assert.AreEqual(expected, actual);
        }

        private void TestIngresDateTest2(string value, TimeSpan expected)
        {
            var actual = IngresDate.ToTimeSpan(GetTestIngresDateTest2(value));
            Assert.AreEqual(expected, actual);
        }

        private void TestIngresDateTest2(DateTime? value, object expected)
        {
            var actual = GetTestIngresDateTest2(value);
            Assert.AreEqual(expected, actual);
        }

        private void TestIngresDateTest2(DateTime? value, DateTime expected)
        {
            var actual = IngresDate.ToDateTime(GetTestIngresDateTest2(value));
            Assert.AreEqual(expected, actual);
        }

        private void TestIngresDateTest2(DateTime? value, TimeSpan expected)
        {
            var actual = IngresDate.ToTimeSpan(GetTestIngresDateTest2(value));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestReadNullDate()
        {
            TestIngresDateTest2("null", null);
        }

        [TestMethod]
        public void TestReadEmptyDate()
        {
            TestIngresDateTest2("date('')", IngresDate.EmptyDateTimeValue);
        }

        [TestMethod]
        public void TestReadDate()
        {
            TestIngresDateTest2("date('2011-12-02')", new DateTime(2011, 12, 2));
        }

        [TestMethod]
        public void TestReadDateTime()
        {
            TestIngresDateTest2("date('2011-12-02 09:53:28')", new DateTime(2011, 12, 2, 9, 53, 28));
        }

        [TestMethod]
        public void TestReadInterval()
        {
            TestIngresDateTest2("date('59 hours 53 minutes 28 seconds')", new TimeSpan(59, 53, 28));
        }

        [TestMethod]
        public void TestReadNegativeInterval()
        {
            TestIngresDateTest2("date('-59 hours 53 minutes 28 seconds')", new TimeSpan(-59, 53, 28));
        }



        [TestMethod]
        public void TestReadInsertedNullDate()
        {
            TestIngresDateTest2(default(DateTime?), null);
        }

        [TestMethod]
        public void TestReadInsertedEmptyDate()
        {
            TestIngresDateTest2(new DateTime(9999, 12, 31, 23, 59, 59), IngresDate.EmptyDateTimeValue);
        }

        [TestMethod]
        public void TestReadInsertedDate()
        {
            TestIngresDateTest2(new DateTime(2011, 12, 2), new DateTime(2011, 12, 2));
        }

        [TestMethod]
        public void TestReadInsertedDateTime()
        {
            TestIngresDateTest2(new DateTime(2011, 12, 2, 9, 53, 28), new DateTime(2011, 12, 2, 9, 53, 28));
        }

        [TestMethod]
        public void TestReadInsertedInterval()
        {
            var expected = new TimeSpan(59, 53, 28);
            TestIngresDateTest2(IngresDate.IntervalDateTimeValue + expected, expected);
        }

        [TestMethod]
        public void TestReadInsertedNegativeInterval()
        {
            var expected = new TimeSpan(-59, 53, 28);
            TestIngresDateTest2(IngresDate.IntervalDateTimeValue + expected, expected);
        }

        
        [TestMethod]
        public void TestReadInsertedEmptyDateUTC()
        {
            TestIngresDateTest2(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc), IngresDate.EmptyDateTimeValue);
        }

        [TestMethod]
        public void TestReadInsertedDate1UTC()
        {
            TestIngresDateTest2(new DateTime(2011, 12, 1, 23, 0, 0, DateTimeKind.Utc), new DateTime(2011, 12, 2, 0, 0, 0));
        }

        [TestMethod]
        public void TestReadInsertedDate2UTC()
        {
            TestIngresDateTest2(new DateTime(2011, 12, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2011, 12, 2, 1, 0, 0));
        }

        [TestMethod]
        public void TestReadInsertedDateTimeUTC()
        {
            TestIngresDateTest2(new DateTime(2011, 12, 2, 9, 53, 28, DateTimeKind.Utc), new DateTime(2011, 12, 2, 10, 53, 28));
        }


        [TestMethod]
        public void TestDateTypes()
        {
            var result = new object[4];
            using (var connection = TestHelper.CreateDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM DateTypesTest";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reader.GetValues(result);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestFormats()
        {
            IngresDate idate;

            idate = new IngresDate(new DateTime(2011, 12, 21, 11, 28, 08));
            var fmt1 = string.Format("{0:dd.MM.yyyy}", idate);
            var fmt2 = string.Format("{0:dd.MM.yyyy hh:mm:ss}", idate);

            Assert.AreEqual<string>("21.12.2011", fmt1);
            Assert.AreEqual<string>("21.12.2011 11:28:08", fmt2);
        }

        [TestMethod]
        public void TestSerializeIngresDate()
        {
            var iDate = new IngresDate(new DateTime(2011, 12, 21, 11, 28, 08));
            IngresDate iDate2;

            var iEmptyDate = IngresDate.Empty;
            IngresDate iEmptyDate2;

            var iInterval = new IngresDate(new TimeSpan(13, 48, 14));
            IngresDate iInterval2;

            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, iDate);
                stream.Seek(0, SeekOrigin.Begin);
                iDate2 = (IngresDate)formatter.Deserialize(stream);
            }

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, iEmptyDate);
                stream.Seek(0, SeekOrigin.Begin);
                iEmptyDate2 = (IngresDate)formatter.Deserialize(stream);
            }

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, iInterval);
                stream.Seek(0, SeekOrigin.Begin);
                iInterval2 = (IngresDate)formatter.Deserialize(stream);
            }

            Assert.AreEqual<IngresDate>(iDate, iDate2);
            Assert.AreEqual<IngresDate>(iEmptyDate, iEmptyDate2);
            Assert.AreEqual<IngresDate>(iInterval, iInterval2);
        }

        [TestMethod]
        public void TestImplicitConversions()
        {
            var now = DateTime.Now;
            IngresDate idateNow = now;
            Assert.AreEqual(idateNow, now);

            var interval = DateTime.Now - now;
            IngresDate iinterval = interval;
            Assert.AreEqual(iinterval, interval);
        }
    }
}
