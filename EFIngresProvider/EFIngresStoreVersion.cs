using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EFIngresProvider
{
    /// <summary>
    /// This enum describes the current server version
    /// </summary>
    public class EFIngresStoreVersion : IComparable<EFIngresStoreVersion>
    {
        public static readonly IEnumerable<EFIngresStoreVersion> Versions = new List<EFIngresStoreVersion>
        {
            new EFIngresStoreVersion( 9, 2, 1, "Ingres"),
            new EFIngresStoreVersion( 9, 2, 0, "Ingres 9.2"),
            new EFIngresStoreVersion( 9, 2, 1, "Ingres 9.2.1"),
            new EFIngresStoreVersion( 9, 2, 3, "Ingres 9.2.3"),
            new EFIngresStoreVersion(10, 0, 0, "Ingres 10.0"),
        };

        public static EFIngresStoreVersion Default
        {
            get { return Versions.OrderBy(x => x).FirstOrDefault(); }
        }

        public EFIngresStoreVersion(int majorVersion, int minorVersion, int microVersion, string token = null)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            MicroVersion = microVersion; 
            Token = token;
        }

        public int MajorVersion { get; private set; }
        public int MinorVersion { get; private set; }
        public int MicroVersion { get; private set; }
        public string Token { get; private set; }

        public override string ToString()
        {
            return string.Format("Ingres {0}.{1}.{2} - {3}", MajorVersion, MinorVersion, MicroVersion, Token);
        }

        public int CompareTo(EFIngresStoreVersion other)
        {
            if (MajorVersion < other.MajorVersion) { return -1; }
            if (MajorVersion > other.MajorVersion) { return  1; }
            if (MinorVersion < other.MinorVersion) { return -1; }
            if (MinorVersion > other.MinorVersion) { return  1; }
            if (MicroVersion < other.MicroVersion) { return -1; }
            if (MicroVersion > other.MicroVersion) { return  1; }
            return 0;
        }
    }

    public static class EFIngresStoreVersionUtils
    {
        /// <summary>
        /// Get the EFIngresStoreVersion from the connection.
        /// </summary>
        /// <param name="connection">current sql connection</param>
        /// <returns>EFIngresStoreVersion corresponding to the current connection</returns>
        public static EFIngresStoreVersion GetStoreVersion(EFIngresConnection connection)
        {
            // IngresConnection.ServerVersion should be something like: "09.02.0001 II 9.2.1 (a64.lnx/103)NPTL"
            var match = Regex.Match(connection.ServerVersion, @"II (\w+)\.(\w+)\.(\w+)");
            if (match.Success)
            {
                var serverVersion = new EFIngresStoreVersion(int.Parse(match.Groups[1].Value),
                                                             int.Parse(match.Groups[2].Value),
                                                             int.Parse(match.Groups[3].Value));
                var version = EFIngresStoreVersion.Versions
                                                  .Where(x => x.CompareTo(serverVersion) <= 0)
                                                  .OrderByDescending(x => x)
                                                  .FirstOrDefault();
                if (version != null) { return version; }
            }
            throw new ArgumentException("The version of Ingres [ " + connection.ServerVersion + " ] is not supported via Ingres Entity Framework provider.");
        }

        public static EFIngresStoreVersion FindStoreVersion(string manifestToken)
        {
            return EFIngresStoreVersion.Versions
                                       .Where(x => x.Token.Equals(manifestToken, StringComparison.InvariantCultureIgnoreCase))
                                       .FirstOrDefault();
        }

        public static EFIngresStoreVersion GetStoreVersion(string manifestToken)
        {
            var version = FindStoreVersion(manifestToken);
            if (version != null) { return version; }
            throw new ArgumentException("Could not determine storage version for provider manifest token [" + manifestToken + "]; a valid provider manifest token is required.");
        }
    }
}
