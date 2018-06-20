using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Ingres.Client;

namespace EFIngresProvider
{
    public partial class EFIngresProviderFactory : DbProviderFactory, IServiceProvider
    {
        /// <summary>
        /// A singleton object for the entity client provider factory object
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "EFIngresProviderFactory implements the singleton pattern and it's stateless. This is needed in order to work with DbProviderFactories.")]
        public static readonly EFIngresProviderFactory Instance = new EFIngresProviderFactory();

        /// <summary>
        /// Constructs the EFIngresProviderFactory object, this is private as users shouldn't create it directly
        /// </summary>
        public EFIngresProviderFactory()
        {
        }

        /// <summary>
        /// Creates a EFIngresCommand object and returns it
        /// </summary>
        /// <returns>A EFIngresCommand object</returns>
        public override DbCommand CreateCommand()
        {
            return new EFIngresCommand();
        }

        /// <summary>
        /// Creates a EFIngresCommandBuilder object and returns it
        /// </summary>
        /// <returns>A EFIngresCommandBuilder object</returns>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            return new IngresCommandBuilder();
        }

        /// <summary>
        /// Creates a EFIngresConnection object and returns it
        /// </summary>
        /// <returns>A EFIngresConnection object</returns>
        public override DbConnection CreateConnection()
        {
            return new EFIngresConnection();
        }

        /// <summary>
        /// Creates a IngresConnectionStringBuilder object and returns it
        /// </summary>
        /// <returns>A IngresConnectionStringBuilder object</returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return new EFIngresConnectionStringBuilder();
        }

        /// <summary>
        /// Creates a IngresDataAdapter object and returns it
        /// </summary>
        /// <returns>A IngresDataAdapter object</returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return new IngresDataAdapter();
        }

        /// <summary>
        /// Creates a IngresParameter object and returns it
        /// </summary>
        /// <returns>A IngresParameter object</returns>
        public override DbParameter CreateParameter()
        {
            return new IngresParameter();
        }

        public override bool CanCreateDataSourceEnumerator
        {
            get { return false; }
        }

        public override System.Security.CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state)
        {
            return new IngresPermission(state);
        }

        /// <summary>
        /// Extension mechanism for additional services
        /// </summary>
        /// <returns>requested service provider or null.</returns>
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(DbProviderServices))
                return EFIngresProviderServices.Instance;
            else
                return null;
        }
    }
}
