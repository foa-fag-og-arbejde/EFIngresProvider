using EFIngresProviderDeploy.Reg;
using System.Collections.Generic;
using System.Linq;

namespace EFIngresProviderDeploy
{
    public class EFIngresProviderReg
    {
        public const string RegPath = @"SOFTWARE\Microsoft\.NETFramework";
        public const string RegPath64 = @"SOFTWARE\Wow6432Node\Microsoft\.NETFramework";

        public static IEnumerable<string> Versions = new[] {
            "v4.0"
        };

        public static RegKey GetEFIngresProviderKey()
        {
            var key = new RegKey("EFIngresProvider", new
            {
                _ = DeployUtils.GetDeployDir()
            });
            return key;
        }

        public static void Install()
        {
            ConsoleLog.WriteLine("Registering EFIngresProvider");
            using (ConsoleLog.Indent())
            {
                foreach (var assemblyFoldersExKey in GetAssemblyFoldersExKeys())
                {
                    ConsoleLog.WriteLine(@"Registering EFIngresProvider to {0}", assemblyFoldersExKey);
                    GetEFIngresProviderKey().Save(assemblyFoldersExKey.RegistryKey);
                }
            }
        }

        public static IEnumerable<WrappedRegistryKey> GetAssemblyFoldersExKeys()
        {
            return Enumerable.Concat(
                GetAssemblyFoldersExKeys(WrappedRegistry.LocalMachine, RegPath),
                GetAssemblyFoldersExKeys(WrappedRegistry.LocalMachine, RegPath64)
            );
        }

        private static IEnumerable<WrappedRegistryKey> GetAssemblyFoldersExKeys(WrappedRegistryKey root, string regPath)
        {
            using (var rootKey = WrappedRegistry.OpenSubKey(root, regPath, false))
            {
                if (rootKey != null)
                {
                    foreach (var version in Versions)
                    {
                        var versionKey = WrappedRegistry.OpenSubKey(rootKey, version, false);
                        var assemblyFoldersExKey = WrappedRegistry.OpenSubKey(versionKey, "AssemblyFoldersEx", true);
                        if (versionKey != null && assemblyFoldersExKey != null)
                        {
                            yield return assemblyFoldersExKey;
                        }
                    }
                }
            }
        }
    }
}
