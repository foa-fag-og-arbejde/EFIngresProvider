using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.Common;
using System.Reflection;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public class EntityFrameworkPrerequisiteTests : TestBase
    {
        [TestMethod]
        public void GetFactoryViaConnection()
        {
            var factory = TestHelper.GetFactoryViaDbProviderFactories();
            var connection = factory.CreateConnection();
            var property = typeof(DbConnection).GetProperty("ProviderFactory", BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic);
            var factoryFromConnection = (DbProviderFactory)property.GetValue(connection, null);

            Assert.IsNotNull(factoryFromConnection, "Connection.ProviderFactory returned null");
            Assert.IsInstanceOfType(factoryFromConnection, factory.GetType(), "Connection.ProviderFactory");
        }

        [TestMethod]
        public void VerifyCommandImplementsICloneable()
        {
            var factory = TestHelper.GetFactoryViaDbProviderFactories();
            var command = factory.CreateCommand();
            Assert.IsInstanceOfType(command, typeof(ICloneable), "factory.CreateCommand()");

            var cloneable = command as ICloneable;
            Assert.IsNotNull(cloneable);

            var clonedCommand = cloneable.Clone() as DbCommand;
            Assert.IsNotNull(clonedCommand);
            Assert.IsInstanceOfType(clonedCommand, typeof(DbCommand));
        }

        [TestMethod]
        public void VerifyProviderSupportsDbProviderServices()
        {
            var factory = TestHelper.GetFactoryViaDbProviderFactories();
            var iserviceProvider = factory as IServiceProvider;
            Assert.IsNotNull(iserviceProvider);
            Assert.IsInstanceOfType(iserviceProvider, typeof(IServiceProvider));

            var dbProviderServices = iserviceProvider.GetService(typeof(DbProviderServices)) as DbProviderServices;
            Assert.IsNotNull(dbProviderServices);
            Assert.IsInstanceOfType(dbProviderServices, typeof(DbProviderServices));
        }

        [TestMethod]
        public void VerifyProviderManifest()
        {
            var providerservices = TestHelper.GetProviderServicesViaDbProviderFactories();
            var manifest = providerservices.GetProviderManifest("Ingres 9.2.1");
            Assert.IsNotNull(manifest);
            Assert.IsInstanceOfType(manifest, typeof(DbProviderManifest));
        }

        [TestMethod]
        public void VerifyProviderManifestToken()
        {
            var factory = TestHelper.GetFactoryViaDbProviderFactories();
            var providerservices = TestHelper.GetProviderServicesViaDbProviderFactories(factory);
            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = TestHelper.DirectConnectionString;

                var token = providerservices.GetProviderManifestToken(connection);
                var version = EFIngresStoreVersionUtils.FindStoreVersion(token);
                Assert.IsNotNull(version);
            }
        }
    }
}
