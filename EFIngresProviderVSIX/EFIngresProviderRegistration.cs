using EFIngresDDEXProvider;
using Microsoft.VisualStudio.Data.Services.SupportEntities;
using Microsoft.VisualStudio.Shell;
using System;

namespace EFIngresProviderVSIX
{
    class EFIngresProviderRegistration : RegistrationAttribute
    {
        public const string DataProviderGuid = "C1B3C7B5-205D-4231-A359-112CE13D0FB9";
        public const string DataSourceGuid = "E4813FAC-B18C-46C9-AE36-D9CDC31E3A3A";

        public override void Register(RegistrationContext context)
        {
            try
            {
                Unregister(context);
                RegisterDataProvider(context);
                RegisterDataSource(context);
            }
            catch (Exception)
            {
                Unregister(context);
                throw;
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey($@"DataProviders\{{{DataProviderGuid}}}");
            context.RemoveKey($@"DataSources\{{{DataSourceGuid}}}");
        }

        private void RegisterDataProvider(RegistrationContext context)
        {
            using (var providerKey = context.CreateKey($@"DataProviders\{{{DataProviderGuid}}}"))
            using (var supportedObjectsKey = providerKey.CreateSubkey("SupportedObjects"))
            {
                providerKey.SetValue(null, Constants.InvariantName);
                providerKey.SetValue("InvariantName", Constants.InvariantName);
                providerKey.SetValue("AssociatedSource", DataSourceGuid);
                providerKey.SetValue("Description", "Provider_Description = EFIngresDDEXProvider.Properties.Resources");
                providerKey.SetValue("DisplayName", "Provider_DisplayName = EFIngresDDEXProvider.Properties.Resources");
                providerKey.SetValue("ShortDisplayName", "Provider_ShortDisplayName = EFIngresDDEXProvider.Properties.Resources");
                providerKey.SetValue("PlatformVersion", "4.0");
                providerKey.SetValue("FactoryService", $"{{{EFIngresProviderObjectFactory.Guid}}}");
                providerKey.SetValue("Technology", "{77AB9A9D-78B9-4ba7-91AC-873F5338F1D2}");

                supportedObjectsKey.CreateSubkey(nameof(IVsDataConnectionSupport));
                supportedObjectsKey.CreateSubkey(nameof(IVsDataConnectionUIControl));
                supportedObjectsKey.CreateSubkey(nameof(IVsDataConnectionProperties));
                supportedObjectsKey.CreateSubkey(nameof(IVsDataConnectionUIProperties));
                supportedObjectsKey.CreateSubkey(nameof(IVsDataConnectionEquivalencyComparer));
                supportedObjectsKey.CreateSubkey(nameof(IVsDataSourceInformation));
                supportedObjectsKey.CreateSubkey(nameof(IVsDataObjectSupport));
                supportedObjectsKey.CreateSubkey(nameof(IVsDataViewSupport));
            }
        }

        private void RegisterDataSource(RegistrationContext context)
        {
            using (var dataSourceKey = context.CreateKey($@"DataSources\{{{DataSourceGuid}}}"))
            using (var supportingProvidersKey = dataSourceKey.CreateSubkey("SupportingProviders"))
            {
                dataSourceKey.SetValue(null, "Ingres Data Source");
                dataSourceKey.SetValue("DefaultProvider", DataProviderGuid);
                supportingProvidersKey.CreateSubkey(DataProviderGuid);
            }
        }
    }
}
