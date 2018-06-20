using Microsoft.VisualStudio.Data.Framework;
using Microsoft.VisualStudio.Data.Framework.AdoDotNet;
using Microsoft.VisualStudio.Data.Services.SupportEntities;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace EFIngresDDEXProvider
{
    [Guid(Guid)]
    public class EFIngresProviderObjectFactory : DataProviderObjectFactory
    {
        public const string Guid = "6363663C-6295-4D63-A47B-468CBBCA49CB";

        public override object CreateObject(Type objType)
        {
            if (objType == typeof(IVsDataConnectionSupport))
                return new AdoDotNetConnectionSupport();
            if (objType == typeof(IVsDataConnectionProperties) || objType == typeof(IVsDataConnectionUIProperties))
                return new AdoDotNetConnectionProperties();
            if (objType == typeof(IVsDataSourceInformation))
                return new EFIngresSourceInformation();
            if (objType == typeof(IVsDataObjectSupport))
                return new DataObjectSupport($"{GetType().Namespace}.EFIngresObjectSupport", Assembly.GetExecutingAssembly());
            if (objType == typeof(IVsDataViewSupport))
                return new DataViewSupport($"{GetType().Namespace}.EFIngresViewSupport", Assembly.GetExecutingAssembly());
            if (objType == typeof(IVsDataConnectionEquivalencyComparer))
                return new EFIngresConnectionEquivalencyComparer();
            return null;
        }
    }
}
