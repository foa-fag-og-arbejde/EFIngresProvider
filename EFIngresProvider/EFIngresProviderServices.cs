using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Data.Common.CommandTrees;
using System.Diagnostics;
using EFIngresProvider.Helpers;
using System.Data.Metadata.Edm;
using Ingres.Client;
using System.Data;
using EFIngresProvider.SqlGen;

namespace EFIngresProvider
{
    /// <summary>
    /// The ProviderServices object for the EFIngresProvider
    /// </summary>
    internal class EFIngresProviderServices : DbProviderServices
    {
        internal static readonly EFIngresProviderServices Instance = new EFIngresProviderServices();

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest manifest, DbCommandTree commandTree)
        {
            DbCommand prototype = CreateCommand(manifest, commandTree);
            DbCommandDefinition result = CreateCommandDefinition(prototype);
            return result;
        }

        /// <summary>
        /// Create an EFIngresCommand object, given the provider manifest and command tree
        /// </summary>
        private DbCommand CreateCommand(DbProviderManifest manifest, DbCommandTree commandTree)
        {
            if (commandTree == null) throw new ArgumentNullException("commandTree");

            var ingresManifest = Cast<EFIngresProviderManifest>(manifest, "manifest");

            var version = ingresManifest.Version;

            var command = new EFIngresCommand();

            List<DbParameter> parameters;
            CommandType commandType;

            command.CommandText = SqlGenerator.GenerateSql(commandTree, version, out parameters, out commandType);
            command.CommandType = commandType;

            if (command.CommandType == CommandType.Text)
            {
                //command.CommandText += Environment.NewLine + Environment.NewLine + "-- provider: " + this.GetType().Assembly.FullName;
            }

            // Get the function (if any) implemented by the command tree since this influences our interpretation of parameters
            EdmFunction function = null;
            if (commandTree is DbFunctionCommandTree)
            {
                function = ((DbFunctionCommandTree)commandTree).EdmFunction;
            }

            // Now make sure we populate the command's parameters from the CQT's parameters:
            command.Parameters.AddRange(commandTree.Parameters.Select(queryParameter => CreateDbParameter(queryParameter.Key, queryParameter.Value, ParameterMode.In, DBNull.Value)).ToArray());

            // Now add parameters added as part of SQL gen (note: this feature is only safe for DML SQL gen which
            // does not support user parameters, where there is no risk of name collision)
            if (parameters != null && parameters.Count > 0)
            {
                if (!(commandTree is DbModificationCommandTree))
                {
                    throw new InvalidOperationException("SqlGenParametersNotPermitted");
                }

                command.Parameters.AddRange(parameters.ToArray());
            }

            return command;
        }

        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            EFIngresStoreVersion version = null;
            UsingConnection(connection, conn =>
            {
                version = EFIngresStoreVersionUtils.GetStoreVersion(conn);
            });

            return version.Token;
        }

        protected override DbProviderManifest GetDbProviderManifest(string versionHint)
        {
            if (string.IsNullOrEmpty(versionHint))
            {
                throw new ArgumentException("Could not determine store version; a valid store connection or a version hint is required.");
            }

            return new EFIngresProviderManifest(versionHint);
        }

        protected override string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection)
        {
            if (providerManifestToken == null)
                throw new ArgumentNullException("providerManifestToken must not be null");

            if (storeItemCollection == null)
                throw new ArgumentNullException("storeItemCollection must not be null");

            return DdlBuilder.CreateObjectsScript(storeItemCollection);
        }

        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            throw new NotSupportedException();
        }

        private static string GetDatabaseName(EFIngresConnection ingresConnection)
        {
            string databaseName = ingresConnection.Database;
            if (string.IsNullOrEmpty(databaseName))
                throw new InvalidOperationException("Connection String did not specify an Initial Catalog");

            return databaseName;
        }

        private static void GetDatabaseFileNames(EFIngresConnection connection, out string dataFileName, out string logFileName)
        {
            throw new NotSupportedException();
        }

        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            if (storeItemCollection == null) throw new ArgumentNullException("storeItemCollection must not be null");

            var exists = false;
            UsingMasterConnection(connection, conn =>
            {
                var databaseName = GetDatabaseName(conn);
                var storeVersion = EFIngresStoreVersionUtils.GetStoreVersion(conn);
                var databaseExistsScript = DdlBuilder.CreateDatabaseExistsScript(databaseName);

                int result = (int)CreateCommand(conn, databaseExistsScript, commandTimeout).ExecuteScalar();
                exists = (result == 1);
            });
            return exists;
        }

        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            throw new NotSupportedException();
        }

        private static EFIngresCommand CreateCommand(EFIngresConnection connection, string commandText, int? commandTimeout)
        {
            Debug.Assert(connection != null);
            var command = new EFIngresCommand(commandText, connection);
            if (commandTimeout.HasValue)
            {
                command.CommandTimeout = commandTimeout.Value;
            }
            return command;
        }

        private static T Cast<T>(object obj, string parameterName = "obj") where T : class
        {
            if (obj == null) throw new ArgumentException(parameterName);
            var result = obj as T;
            if (result == null)
            {
                throw new ArgumentException(string.Format("{0} is not of type '{1}'.", parameterName, typeof(T).Name));
            }
            return result;
        }

        private static EFIngresConnection GetIngresConnection(DbConnection connection)
        {
            var ingresConnection = Cast<EFIngresConnection>(connection);
            if (string.IsNullOrWhiteSpace(ingresConnection.ConnectionString))
            {
                throw new ArgumentException("Could not determine storage version; a valid storage connection or a version hint is required.");
            }

            return ingresConnection;
        }

        private static void UsingConnection(DbConnection connection, Action<EFIngresConnection> act)
        {
            UsingConnection(GetIngresConnection(connection), act);
        }

        private static void UsingConnection(EFIngresConnection connection, Action<EFIngresConnection> act)
        {
            // remember the connection string so that we can reset it if credentials are wiped
            var holdConnectionString = connection.ConnectionString;
            var openingConnection = connection.State == ConnectionState.Closed;
            if (openingConnection)
            {
                connection.Open();
            }
            try
            {
                act(connection);
            }
            finally
            {
                if (openingConnection && connection.State == ConnectionState.Open)
                {
                    // if we opened the connection, we should close it
                    connection.Close();
                }
                if (connection.ConnectionString != holdConnectionString)
                {
                    connection.ConnectionString = holdConnectionString;
                }
            }
        }

        private static void UsingMasterConnection(DbConnection connection, Action<EFIngresConnection> act)
        {
            UsingMasterConnection(GetIngresConnection(connection), act);
        }

        private static void UsingMasterConnection(EFIngresConnection connection, Action<EFIngresConnection> act)
        {
            var connectionBuilder = new IngresConnectionStringBuilder(connection.ConnectionString) { Database = "iidbdb" };

            using (var masterConnection = new EFIngresConnection(connectionBuilder.ConnectionString))
            {
                UsingConnection(masterConnection, act);
            }
        }

        /// <summary>
        /// Creates a SqlParameter given a name, type, and direction
        /// </summary>
        internal static DbParameter CreateDbParameter(string name, TypeUsage type, ParameterMode mode, object value)
        {
            var result = new IngresParameter(name, value)
            {
                Direction = MetadataHelpers.ParameterModeToParameterDirection(mode),
                IngresType = GetIngresType(type),
                IsNullable = MetadataHelpers.IsNullable(type)
            };

            result.Size = GetParameterSize(type, mode != ParameterMode.In) ?? result.Size;
            return result;
        }


        /// <summary>
        /// Determines IngresType for the given primitive type. Extracts facet
        /// information as well.
        /// </summary>
        private static IngresType GetIngresType(TypeUsage type)
        {
            // only supported for primitive type
            var primitiveTypeKind = MetadataHelpers.GetPrimitiveTypeKind(type);

            switch (primitiveTypeKind)
            {
                case PrimitiveTypeKind.Binary:
                    return GetBinaryDbType(type);

                case PrimitiveTypeKind.Boolean:
                    return IngresType.Int;

                case PrimitiveTypeKind.Byte:
                    return IngresType.TinyInt;

                case PrimitiveTypeKind.Time:
                    return IngresType.Time;

                case PrimitiveTypeKind.DateTimeOffset:
                    throw new Exception("unknown PrimitiveTypeKind " + primitiveTypeKind);

                case PrimitiveTypeKind.DateTime:
                    return IngresType.DateTime;

                case PrimitiveTypeKind.Decimal:
                    return IngresType.Decimal;

                case PrimitiveTypeKind.Double:
                    return IngresType.Double;

                case PrimitiveTypeKind.Guid:
                    throw new Exception("unknown PrimitiveTypeKind " + primitiveTypeKind);

                case PrimitiveTypeKind.Int16:
                    return IngresType.SmallInt;

                case PrimitiveTypeKind.Int32:
                    return IngresType.Int;

                case PrimitiveTypeKind.Int64:
                    return IngresType.BigInt;

                case PrimitiveTypeKind.SByte:
                    return IngresType.TinyInt;

                case PrimitiveTypeKind.Single:
                    return IngresType.Real;

                case PrimitiveTypeKind.String:
                    return GetStringDbType(type);

                default:
                    Debug.Fail("unknown PrimitiveTypeKind " + primitiveTypeKind);
                    throw new Exception("unknown PrimitiveTypeKind " + primitiveTypeKind);
            }
        }

        /// <summary>
        /// Determines preferred value for SqlParameter.Size. Returns null
        /// where there is no preference.
        /// </summary>
        private static int? GetParameterSize(TypeUsage type, bool isOutParam)
        {
            int maxLength;
            if (MetadataHelpers.TryGetMaxLength(type, out maxLength))
            {
                // if the MaxLength facet has a specific value use it
                return maxLength;
            }
            else if (isOutParam)
            {
                // if the parameter is a return/out/inout parameter, ensure there 
                // is space for any value
                return int.MaxValue;
            }
            else
            {
                // no value
                return default(int?);
            }
        }

        /// <summary>
        /// Chooses the appropriate IngresType for the given string type.
        /// </summary>
        private static IngresType GetStringDbType(TypeUsage type)
        {
            Debug.Assert(type.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType &&
                PrimitiveTypeKind.String == ((PrimitiveType)type.EdmType).PrimitiveTypeKind, "only valid for string type");

            IngresType dbType;
            if (type.EdmType.Name.ToLowerInvariant() == "xml")
            {
                dbType = IngresType.LongVarChar;
            }
            else
            {
                // Specific type depends on whether the string is a unicode string and whether it is a fixed length string.
                // By default, assume widest type (unicode) and most common type (variable length)
                bool unicode;
                bool fixedLength;
                if (!MetadataHelpers.TryGetIsFixedLength(type, out fixedLength))
                {
                    fixedLength = false;
                }

                if (!MetadataHelpers.TryGetIsUnicode(type, out unicode))
                {
                    unicode = true;
                }

                if (fixedLength)
                {
                    dbType = (unicode ? IngresType.NChar : IngresType.Char);
                }
                else
                {
                    dbType = (unicode ? IngresType.NVarChar : IngresType.VarChar);
                }
            }
            return dbType;
        }

        /// <summary>
        /// Chooses the appropriate IngresType for the given binary type.
        /// </summary>
        private static IngresType GetBinaryDbType(TypeUsage type)
        {
            Debug.Assert(type.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType &&
                PrimitiveTypeKind.Binary == ((PrimitiveType)type.EdmType).PrimitiveTypeKind, "only valid for binary type");

            // Specific type depends on whether the binary value is fixed length. By default, assume variable length.
            bool fixedLength;
            if (!MetadataHelpers.TryGetIsFixedLength(type, out fixedLength))
            {
                fixedLength = false;
            }

            return fixedLength ? IngresType.Binary : IngresType.VarBinary;
        }
    }
}
