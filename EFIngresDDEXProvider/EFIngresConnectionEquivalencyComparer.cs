using Microsoft.VisualStudio.Data.Framework;
using Microsoft.VisualStudio.Data.Services.SupportEntities;

namespace EFIngresDDEXProvider
{
    public class EFIngresConnectionEquivalencyComparer : DataConnectionEquivalencyComparer
    {
        protected override bool AreEquivalent(IVsDataConnectionProperties connectionProperties1, IVsDataConnectionProperties connectionProperties2)
            => connectionProperties1["Server"].ToString() == connectionProperties2["Server"].ToString()
            && connectionProperties1["Port"].ToString() == connectionProperties2["Port"].ToString()
            && connectionProperties1["Database"].ToString() == connectionProperties2["Database"].ToString()
            && connectionProperties1["User ID"].ToString() == connectionProperties2["User ID"].ToString()
            && connectionProperties1["Password"].ToString() == connectionProperties2["Password"].ToString()
            ;
    }
}
