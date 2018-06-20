using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml.Linq;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class InstallTests
    {
        private TException Try<TException>(Action action)
            where TException: Exception
        {
            try
            {
                action();
                return null;
            }
            catch (TException ex)
            {
                return ex;
            }
        }

        [TestMethod]
        public void InstallNonConfig()
        {
            // Arrange
            var doc = XDocument.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?>
<slam>
</slam>
");

            // Act
            var actual = Try<ArgumentException>(() => EFIngresProviderInstaller.Install("Test", doc, true, true));

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void InstallEmptyConfig()
        {
            // Arrange
            var doc = XDocument.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
</configuration>
");

            // Act
            var actual = EFIngresProviderInstaller.Install("Test", doc, true, true);

            Console.WriteLine(doc.ToString(SaveOptions.None));

            // Assert
            Assert.IsTrue(actual);
        }
    }
}
