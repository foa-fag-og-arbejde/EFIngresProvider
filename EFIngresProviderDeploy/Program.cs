using EFIngresProvider;
using System.Configuration;
using System.IO;

namespace EFIngresProviderDeploy
{
    class Program
    {
        static void Main(string[] args)
        {
            InstallProviderToConfig(GetMachineConfigFilePath(), false);
            DDEXReg.Install();
            EFIngresProviderReg.Install();

            GacUninstall("EFIngresProvider");
            GacInstall("EFIngresProvider.dll");
            GacInstall("Ingres.Client.dll");
        }

        /// <summary>
        /// Gets the path to machine.config
        /// </summary>
        /// <returns>The path to machine.config</returns>
        private static string GetMachineConfigFilePath()
        {
            return ConfigurationManager.OpenMachineConfiguration().FilePath;
        }

        private static void InstallProviderToConfig(string configFile, bool removeFirst)
        {
            ConsoleLog.WriteLine("Installing EFIngresProvider to {0}", Path.GetFileName(configFile));
            using (ConsoleLog.Indent())
            {
                ConsoleLog.WriteLine("File: {0}", configFile);
                var changed = EFIngresProviderInstaller.Install(configFile, removeFirst, true);
                if (changed)
                {
                    ConsoleLog.WriteLine("Installed");
                }
                else
                {
                    ConsoleLog.WriteLine("Already installed - no changes written to the file");
                }
            }
        }

        private static void GacInstall(string assemblyFileName)
        {
            var assemblyPath = DeployUtils.GetAssemblyPath(assemblyFileName);
            ConsoleLog.WriteLine("Installing {0} to GAC", Path.GetFileName(assemblyPath));
            using (ConsoleLog.Indent())
            {
                ConsoleLog.WriteLine("Path: {0}", assemblyPath);
                GacUtil.InstallAssembly(assemblyPath, true);
            }
        }

        private static void GacUninstall(string assemblyName)
        {
            ConsoleLog.WriteLine("Uninstalling {0} from GAC", assemblyName);
            using (ConsoleLog.Indent())
            {
                GacUtil.UninstallAssembly(assemblyName);
            }
        }
    }
}
