using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFIngresProviderDeploy.Reg
{
    public static class WrappedRegistry
    {
        static WrappedRegistry()
        {
            Clear();
        }

        private static HashSet<WrappedRegistryKey> _keys = new HashSet<WrappedRegistryKey>();
        public static IEnumerable<WrappedRegistryKey> Keys => _keys;
        private static Dictionary<string, Lazy<WrappedRegistryKey>> _specialKeys = new Dictionary<string, Lazy<WrappedRegistryKey>>();

        public static WrappedRegistryKey CurrentUser => _specialKeys["CurrentUser"].Value;
        public static WrappedRegistryKey LocalMachine => _specialKeys["LocalMachine"].Value;
        public static WrappedRegistryKey Users => _specialKeys["Users"].Value;

        public static void Clear()
        {
            foreach (var key in _keys.ToList())
            {
                key.Dispose();
            }
            _keys.Clear();
            _specialKeys = new Dictionary<string, Lazy<WrappedRegistryKey>>
            {
                { "CurrentUser", new Lazy<WrappedRegistryKey>(() => WrappedRegistryKey.Create(Registry.CurrentUser)) },
                { "LocalMachine", new Lazy<WrappedRegistryKey>(() => WrappedRegistryKey.Create(Registry.LocalMachine)) },
                { "Users", new Lazy<WrappedRegistryKey>(() => WrappedRegistryKey.Create(Registry.Users)) },
            };
        }

        public static WrappedRegistryKey Add(WrappedRegistryKey key)
        {
            if (key == null)
            {
                return null;
            }
            _keys.Add(key);
            return key;
        }

        public static void Remove(WrappedRegistryKey key)
        {
            if (key != null)
            {
                _keys.Remove(key);
            }
        }

        public static WrappedRegistryKey OpenSubKey(WrappedRegistryKey key, string name, bool writable)
        {
            if (key == null)
            {
                return null;
            }
            return key.OpenSubKey(name, writable);
        }
    }
}
