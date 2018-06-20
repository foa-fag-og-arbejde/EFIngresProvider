using EFIngresProviderDeploy.Reg;
using Glob;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EFIngresProviderDeploy
{
    public class DDEXReg
    {
        public const string RegPath = @"SOFTWARE\Microsoft\VisualStudio";
        public const string RegPath64 = @"SOFTWARE\Wow6432Node\Microsoft\VisualStudio";
        public const string DataProviderGUID = "{C1B3C7B5-205D-4231-A359-112CE13D0FB9}";
        public const string DataSourceGUID = "{E4813FAC-B18C-46C9-AE36-D9CDC31E3A3A}";
        public const bool InstallToUsers = true;

        public static readonly RegKey DataSourceKey = _getDataSourceKey();
        private static RegKey _getDataSourceKey()
        {
            var key = new RegKey(DataSourceGUID, new
            {
                _ = "Ingres Data Source",
                DefaultProvider = DataProviderGUID
            });
            var supportingProvidersKey = key.AddSubKey("SupportingProviders");
            supportingProvidersKey.AddSubKey(DataProviderGUID);
            return key;
        }

        public static readonly RegKey DataProviderKey = _getDataProviderKey();
        private static RegKey _getDataProviderKey()
        {
            // The root registry key for the DDEX provider uniquely identifies the
            // provider, supplies various names, and registers the main assembly that
            // implements the provider using a code base.  It also associates the provider
            // with a runtime data technology (ADO .NET) and a specific ADO .NET provider.
            // Finally, it identifies the default DDEX data source associated with this
            // provider, allowing an implicit IVsDataSourceSpecializer support entity
            // implementation to be supplied by the DDEX runtime.
            var key = new RegKey(DataProviderGUID, new
            {
                _ = "EFIngresDDEXProvider",
                AssociatedSource = DataSourceGUID,
                Codebase = DeployUtils.GetAssemblyPath("EFIngresDDEXProvider.dll"),
                Description = "Provider_Description = EFIngresDDEXProvider.Properties.Resources",
                DisplayName = "Provider_DisplayName = EFIngresDDEXProvider.Properties.Resources",
                InvariantName = "EFIngresProvider",
                PlatformVersion = "4.0",
                ShortDisplayName = "Provider_ShortDisplayName = EFIngresDDEXProvider.Properties.Resources",
                Technology = "{77AB9A9D-78B9-4ba7-91AC-873F5338F1D2}",
            });

            // The SupportedObjects registry key allows a provider to specify which set of
            // DDEX support entities it supports in a declarative fashion.  This allows the
            // DDEX runtime to query capabilities of the provider without actually loading
            // it.  The set of support entities currently includes those that can be created
            // as standalone objects such as the IVsDataConnectionProperties support entity,
            // and also those support entities that represent connection services, such as
            // the IVsDataCommand support entity.
            var supportedObjectsKey = key.AddSubKey("SupportedObjects");

            // This registry key indicates the existence of an IDSRefBuilder connection
            // service implementation from the DDEX Framework assembly.  The default value
            // is simply the full name of the type in the assembly that provides the
            // implementation.  Since this is not in the main provider assembly, an
            // Assembly value is used to qualify the type.
            var iDSRefBuilder = supportedObjectsKey.AddSubKey("IDSRefBuilder", new
            {
                _ = "Microsoft.VisualStudio.Data.Framework.DSRefBuilder",
                Assembly = "Microsoft.VisualStudio.Data.Framework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            });

            // This registry key indicates the existence of an IVsDataAsyncCommand
            // connection service implementation that is built into the service provider
            // implemented as part of the AdoDotNetConnectionSupport implementation.  Given
            // that this is the case, the implementation does not need to be specified here.
            supportedObjectsKey.AddSubKey("IVsDataAsyncCommand");
            supportedObjectsKey.AddSubKey("IVsDataCommand");

            // This registry key indicates the existence of an IVsDataConnectionProperties
            // support entity implementation from the main assembly.  The DDEX Framework
            // assembly contains a number of base implementations of support entities for
            // DDEX providers based on a runtime ADO .NET provider; in this case, the
            // connection properties object is implemented in terms of an underlying
            // DbConnectionStringBuilder object supplied by the ADO .NET provider.
            supportedObjectsKey.AddSubKey("IVsDataConnectionProperties", new
            {
                _ = "Microsoft.VisualStudio.Data.Framework.AdoDotNet.AdoDotNetConnectionProperties",
                Assembly = "Microsoft.VisualStudio.Data.Framework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            });
            supportedObjectsKey.AddSubKey("IVsDataConnectionSupport", new
            {
                _ = "Microsoft.VisualStudio.Data.Framework.AdoDotNet.AdoDotNetConnectionSupport",
                Assembly = "Microsoft.VisualStudio.Data.Framework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            });
            supportedObjectsKey.AddSubKey("IVsDataConnectionUIProperties", new
            {
                _ = "Microsoft.VisualStudio.Data.Framework.AdoDotNet.AdoDotNetConnectionProperties",
                Assembly = "Microsoft.VisualStudio.Data.Framework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            });
            supportedObjectsKey.AddSubKey("IVsDataMappedObjectConverter");
            supportedObjectsKey.AddSubKey("IVsDataObjectIdentifierConverter");

            // This registry key indicates the existence of an
            // IVsDataObjectIdentifierResolver connection service implementation that is
            // customized by this provider.  The default value is simply the full name of
            // the type in the main provider assembly.
            supportedObjectsKey.AddSubKey("IVsDataObjectIdentifierResolver", new
            {
                _ = "EFIngresDDEXProvider.EFIngresIdentifierResolver"
            });
            supportedObjectsKey.AddSubKey("IVsDataObjectMemberComparer");
            supportedObjectsKey.AddSubKey("IVsDataObjectSelector", new
            {
                _ = "EFIngresDDEXProvider.EFIngresObjectSelector"
            });

            // This registry key indicates the existence of an IVsDataObjectSupport
            // connection service implementation.  In this sample, a base implementation
            // from the DDEX Framework assembly is used that can be constructed given the
            // name of a resource and an assembly that contains an XML stream.  The DDEX
            // runtime reads the XmlResource and Assembly registry values and calls the
            // constructor with these values.
            supportedObjectsKey.AddSubKey("IVsDataObjectSupport", new
            {
                _ = "Microsoft.VisualStudio.Data.Framework.DataObjectSupport",
                Assembly = "Microsoft.VisualStudio.Data.Framework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                XmlResource = "EFIngresDDEXProvider.EFIngresObjectSupport"
            });

            // This registry key indicates the existence of an IVsDataSourceInformation
            // connection service implementation that is customized by this provider.  The
            // custom code does not, however, supply all the key/value pairs; most are
            // specified here for flexibility and are merged in by the DDEX runtime.
            supportedObjectsKey.AddSubKey("IVsDataSourceInformation", new
            {
                _ = "EFIngresDDEXProvider.EFIngresSourceInformation",
                SupportsAnsi92Sql = "True",
                SupportsQuotedIdentifierParts = "True",
                IdentifierOpenQuote = "\"",
                IdentifierCloseQuote = "\"",
                ServerSeparator = ".",
                CatalogSupported = "True",
                CatalogSupportedInDml = "True",
                SchemaSupported = "True",
                SchemaSupportedInDml = "True",
                SchemaSeparator = ".",
                ParameterPrefix = "@",
                ParameterPrefixInName = "True",
            });
            supportedObjectsKey.AddSubKey("IVsDataTransaction");
            supportedObjectsKey.AddSubKey("IVsDataViewSupport", new
            {
                _ = "Microsoft.VisualStudio.Data.Framework.DataViewSupport",
                Assembly = "Microsoft.VisualStudio.Data.Framework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                XmlResource = "EFIngresDDEXProvider.EFIngresViewSupport"
            });

            return key;
        }

        public static void Install()
        {
            ConsoleLog.WriteLine("Registering EFIngresDDEXProvider");
            using (ConsoleLog.Indent())
            {
                ApplyDDEXKeys(InstallToUsers);
            }
        }

        private static void ApplyDDEXKeys(bool includeUsers)
        {
            ApplyDDEXKeys(WrappedRegistry.LocalMachine);

            // Install in VS >= 2017 using the VSIX package
            //var dir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\VisualStudio"));
            //var hiveFiles = dir.GlobFiles("**/privateregistry.bin").Select(f => f.FullName);
            //foreach (var hiveFile in hiveFiles)
            //{
            //    using (var hive = new Hive(hiveFile))
            //    {
            //        ApplyDDEXKeys(hive.RegistryKey);
            //    }
            //}

            if (includeUsers)
            {
                foreach (var userKeyName in Registry.Users.GetSubKeyNames())
                {
                    ApplyDDEXKeys(WrappedRegistry.OpenSubKey(WrappedRegistry.Users, userKeyName, false));
                }
            }
        }

        private static void ApplyDDEXKeys(WrappedRegistryKey root)
        {
            using (root)
            {
                ApplyDDEXKeys(root, RegPath);
                ApplyDDEXKeys(root, RegPath64);
            }
        }

        private static void ApplyDDEXKeys(WrappedRegistryKey root, string vsRegPath)
        {
            using (var vsRootKey = WrappedRegistry.OpenSubKey(root, vsRegPath, false))
            {
                if (vsRootKey != null)
                {
                    foreach (var vsKeyName in vsRootKey.GetSubKeyNames().Where(x => ValidVsVersion(x)))
                    {
                        using (var ddexKeys = new DDEXKeys(WrappedRegistry.OpenSubKey(vsRootKey, vsKeyName, false)))
                        {
                            ddexKeys.Apply();
                        }
                    }
                }
            }
        }

        private static bool ValidVsVersion(string vsVersion)
        {
            var match = Regex.Match(vsVersion, @"^(\d+)\.\d+(_config)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return Convert.ToInt32(match.Groups[1].Value) >= 10;
            }
            match = Regex.Match(vsVersion, @"^(\d+)\.\d+_[a-f\d]+(_config)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return Convert.ToInt32(match.Groups[1].Value) >= 10;
            }
            return false;
        }

        private class DDEXKeys : IDisposable
        {
            public DDEXKeys(WrappedRegistryKey vsKey)
            {
                if (vsKey != null)
                {
                    VsKey = vsKey;
                    DataProvidersKey = WrappedRegistry.OpenSubKey(vsKey, "DataProviders", true);
                    DataSourcesKey = WrappedRegistry.OpenSubKey(vsKey, "DataSources", true);
                }
            }

            public WrappedRegistryKey VsKey { get; private set; }
            public WrappedRegistryKey DataProvidersKey { get; private set; }
            public WrappedRegistryKey DataSourcesKey { get; private set; }
            public bool IsValid
            {
                get { return VsKey != null && DataProvidersKey != null && DataSourcesKey != null; }
            }

            public void Apply()
            {
                if (IsValid)
                {
                    ConsoleLog.WriteLine($@"Registering EFIngresDDEXProvider to {VsKey}");
                    DataProviderKey.Save(DataProvidersKey.RegistryKey);
                    DataSourceKey.Save(DataSourcesKey.RegistryKey);
                }
            }

            public void Dispose()
            {
                if (VsKey != null)
                {
                    VsKey.Dispose();
                    VsKey = null;
                }
                if (DataProvidersKey != null)
                {
                    DataProvidersKey.Dispose();
                    DataProvidersKey = null;
                }
                if (DataSourcesKey != null)
                {
                    DataSourcesKey.Dispose();
                    DataSourcesKey = null;
                }
            }
        }
    }
}
