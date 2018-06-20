using EFIngresDDEXProvider;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace EFIngresProviderVSIX
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", EFIngresProviderVersion.Version)] // Info on this package for Help/About
    [ProvideBindingPath]  // Necessary for loading EFIngresProvider via DbProviderFactories.GetProvider()
    [ProvideService(typeof(EFIngresProviderObjectFactory), ServiceName = "EFIngresProvider Object Factory")]
    [EFIngresProviderRegistration]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.DataSourceWindowAutoVisible), ProvideAutoLoad(UIContextGuids80.DataSourceWindowSupported)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class EFIngresProviderPackage : Package
    {
        public const string PackageGuidString = "1497AD03-9B40-4543-B534-A4106D82E60B";

        private IServiceContainer ServiceContainer => this;

        protected override void Initialize()
        {
            ServiceContainer.AddService(typeof(EFIngresProviderObjectFactory), new EFIngresProviderObjectFactory(), true);
            SetupEFIngresProviderFactory();
            base.Initialize();
        }

        private void SetupEFIngresProviderFactory()
        {
            var factories = GetDbProviderFactories();
            var existing = factories.Find(Constants.InvariantName);
            if (existing != null)
            {
                factories.Remove(existing);
            }
            // Add an entry for EFIngresProvider
            factories.Add(
                "Ingres Entity Framework Provider",
                ".NET Entity Framework Provider for Ingres",
                Constants.InvariantName,
                "EFIngresProvider.EFIngresProviderFactory, EFIngresProvider"
            );
        }

        private DataRowCollection GetDbProviderFactories()
        {
            if (!(ConfigurationManager.GetSection("system.data") is DataSet systemData))
                throw new Exception("No system.data section found in configuration manager!");

            var index = systemData.Tables.IndexOf("DbProviderFactories");
            return index >= 0
                ? systemData.Tables[index].Rows
                : systemData.Tables.Add("DbProviderFactories").Rows;
        }
    }
}
