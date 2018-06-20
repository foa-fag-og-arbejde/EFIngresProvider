using System.IO;

namespace EFIngresProviderDeploy
{
    public static class DeployUtils
    {
        public static string GetAssemblyPath(string assemblyFileName)
        {
            return Path.Combine(GetDeployDir(), assemblyFileName);
        }

        public static string GetDeployDir()
        {
            return Path.GetDirectoryName(typeof(DeployUtils).Assembly.Location);
        }
    }
}
