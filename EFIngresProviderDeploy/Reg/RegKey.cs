using System.Collections.Generic;
using Microsoft.Win32;

namespace EFIngresProviderDeploy.Reg
{
    public class RegKey
    {
        public RegKey(string name, object values = null)
        {
            Name = name;
            if (values != null)
            {
                foreach (var property in values.GetType().GetProperties())
                {
                    AddValue(property.Name == "_" ? "" : property.Name, property.GetValue(values, null));
                }
            }
        }

        private List<RegValue> _values = new List<RegValue>();
        private List<RegKey> _subKeys = new List<RegKey>();

        public RegKey Parent { get; private set; }
        public string Name { get; set; }
        public IEnumerable<RegValue> Values { get { return _values; } }
        public IEnumerable<RegKey> SubKeys { get { return _subKeys; } }

        public void DeleteFrom(RegistryKey parentKey)
        {
            parentKey.DeleteSubKeyTree(Name, false);
        }

        public void Save(RegistryKey parentKey)
        {
            DeleteFrom(parentKey);
            var key = parentKey.CreateSubKey(Name);
            foreach (var value in Values)
            {
                value.Save(key);
            }
            foreach (var subKey in SubKeys)
            {
                subKey.Save(key);
            }
        }

        public RegKey AddSubKey(string name, object values = null)
        {
            var subKey = new RegKey(name, values)
            {
                Parent = this
            };
            _subKeys.Add(subKey);
            return subKey;
        }

        public void AddValue(string name, object value)
        {
            _values.Add(new RegValue(name, value));
        }
    }
}
