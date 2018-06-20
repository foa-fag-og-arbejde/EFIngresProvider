using static Microsoft.VisualStudio.Shell.RegistrationAttribute;

namespace EFIngresProviderVSIX
{
    public static class RegistrationAttributeKeyExtensions
    {
        public static Key CreateSubkey(this Key parentKey, string name, object values)
        {
            return parentKey.CreateSubkey(name).WithValues(values);
        }

        public static Key CreateKey(this RegistrationContext context, string name, object values)
        {
            return context.CreateKey(name).WithValues(values);
        }

        public static Key WithValues(this Key key, object values)
        {
            if (values != null)
            {
                foreach (var property in values.GetType().GetProperties())
                {
                    key.SetValue(property.Name == "_" ? null : property.Name, property.GetValue(values, null));
                }
            }
            return key;
        }

        public static Key RegisterSupportedObject<T>(this Key supportedObjectsKey, object values = null)
        {
            return supportedObjectsKey.CreateSubkey(typeof(T).Name, values);
        }
    }
}
