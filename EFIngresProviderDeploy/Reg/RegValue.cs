using System;
using Microsoft.Win32;

namespace EFIngresProviderDeploy.Reg
{
    public class RegValue
    {
        public RegValue(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (value is string)
            {
                RegistryValueKind = RegistryValueKind.String;
            }
            else
            {
                RegistryValueKind = RegistryValueKind.Unknown;
            }
            Name = name;
            Value = value;
        }

        public RegistryValueKind RegistryValueKind { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        public void Save(RegistryKey key)
        {
            key.SetValue(Name, Value, RegistryValueKind);
        }
    }
}
