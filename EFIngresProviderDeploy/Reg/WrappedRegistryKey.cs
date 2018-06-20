using Microsoft.Win32;
using System;

namespace EFIngresProviderDeploy.Reg
{
    public class WrappedRegistryKey : IDisposable
    {
        public static WrappedRegistryKey Create(RegistryKey key)
        {
            return key != null ? new WrappedRegistryKey(key) : null;
        }

        private WrappedRegistryKey(RegistryKey registryKey)
        {
            RegistryKey = registryKey;
            WrappedRegistry.Add(this);
        }

        public RegistryKey RegistryKey { get; private set; }

        public string[] GetSubKeyNames()
        {
            return RegistryKey.GetSubKeyNames();
        }

        public WrappedRegistryKey OpenSubKey(string name)
        {
            return Create(RegistryKey.OpenSubKey(name));
        }

        public WrappedRegistryKey OpenSubKey(string name, bool writable)
        {
            return Create(RegistryKey.OpenSubKey(name, writable));
        }

        public override string ToString()
        {
            return RegistryKey.ToString();
        }

        public void Dispose()
        {
            if (RegistryKey != null)
            {
                RegistryKey.Close();
                RegistryKey.Dispose();
                RegistryKey = null;
            }
            WrappedRegistry.Remove(this);
        }
    }
}
