using ProcessPrivileges;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace EFIngresProviderDeploy.Reg
{
    public class Hive : IDisposable
    {
        private const uint HKEY_USERS = 0x80000003;
        private const uint ERROR_SUCCESS = 0;

        [DllImport("advapi32.dll")]
        private static extern int RegLoadKey(uint hKey, string lpSubKey, string lpFile);
        [DllImport("advapi32.dll")]
        private static extern int RegUnLoadKey(uint hKey, string lpSubKey);

        public Hive(string filePath)
        {
            FilePath = filePath;
            AdjustPrivileges();
            var result = RegLoadKey(HKEY_USERS, Name, FilePath);
            if (result != ERROR_SUCCESS)
            {
                throw new Exception($"Could not load hive at {FilePath}\n  Error code: {result} (0X{result:X})");
            }
            _loaded = true;
            ConsoleLog.WriteLine($"Loading registry hive {FilePath}");
            RegistryKey = WrappedRegistry.Users.OpenSubKey(Name);
            RegistryKeyPath = RegistryKey.ToString();
        }

        private bool _loaded = false;
        public string FilePath { get; }
        public string Name => $"VisualStudio.{Path.GetFileName(Path.GetDirectoryName(FilePath))}";
        public int Handle { get; }
        public WrappedRegistryKey RegistryKey { get; private set; }
        public string RegistryKeyPath { get; }

        public static void AdjustPrivileges()
        {
            // Access token handle reused within the using block.
            using (AccessTokenHandle accessTokenHandle = Process.GetCurrentProcess().GetAccessTokenHandle(TokenAccessRights.AdjustPrivileges | TokenAccessRights.Query))
            {
                // Enable privileges using the same access token handle.
                accessTokenHandle.EnablePrivilege(Privilege.Backup);
                accessTokenHandle.EnablePrivilege(Privilege.Restore);
            }
        }

        public void Dispose()
        {
            var before = WrappedRegistry.Keys.ToList();
            WrappedRegistry.Clear();
            var after = WrappedRegistry.Keys.ToList();
            if (RegistryKey != null)
            {
                ConsoleLog.WriteLine($"Closing and disposing RegistryKey");
                RegistryKey.Dispose();
                RegistryKey = null;
            }
            if (_loaded)
            {
                _loaded = false;
                ConsoleLog.WriteLine($"Unloading registry hive {FilePath}");
                AdjustPrivileges();
                var result = RegUnLoadKey(HKEY_USERS, Name);
                if (result != ERROR_SUCCESS)
                {
                    ConsoleLog.WriteLine();
                    ConsoleLog.WriteLine($"Failed to unload registry hive");
                    using (ConsoleLog.Indent())
                    {
                        ConsoleLog.WriteLine($"Hive file: {FilePath}");
                        ConsoleLog.WriteLine($"Error code: {result} (0X{result:X})");
                        ConsoleLog.WriteLine($"This can prevent the version of Visual Studio, that uses the hive, from starting");
                        ConsoleLog.WriteLine($"To resolve this, do the following:");
                        using (ConsoleLog.Indent())
                        {
                            ConsoleLog.WriteLine($"Start RegEdit.exe");
                            ConsoleLog.WriteLine($@"Select the key [{RegistryKeyPath}]");
                            //ConsoleLog.WriteLine($@"Select the key [Computer\HKEY_USERS\{Name}]");
                            ConsoleLog.WriteLine($"Choose [Unload Hive...] from the file menu");
                        }
                    }
                    ConsoleLog.WriteLine();
                }
            }
        }
    }
}
