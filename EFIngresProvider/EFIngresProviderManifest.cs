using System;
using System.Data;
using System.Data.Common;
using System.Data.Metadata.Edm;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Collections.ObjectModel;
using System.Text;

namespace EFIngresProvider
{
    public class EFIngresProviderManifest : DbXmlEnabledProviderManifest
    {
        #region Internal Fields

        //public const string ProviderName = "Ingres Entity Framework Provider";
        //public const string ProviderInvariantName = "EFIngresProvider";
        //public const string Description = ".Net Framework Data Provider for Ingres";
        //public const string FactoryTypeName = "EFIngresProvider.EFIngresProviderFactory, EFIngresProvider";
        //public static readonly string FactoryFullTypeName = typeof(EFIngresProviderFactory).AssemblyQualifiedName;

        #endregion

        #region Private Fields

        /// <summary>
        /// maximum size of sql server unicode 
        /// </summary>
        private const int varcharMaxSize = 8000;
        private const int nvarcharMaxSize = 4000;
        private const int binaryMaxSize = 8000;

        private EFIngresStoreVersion _version = EFIngresStoreVersion.Default;
        private ReadOnlyCollection<PrimitiveType> _primitiveTypes = null;
        private ReadOnlyCollection<EdmFunction> _functions = null;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manifestToken">A token used to infer the capabilities of the store</param>
        public EFIngresProviderManifest(string manifestToken)
            : base(GetProviderManifest())
        {
            _version = EFIngresStoreVersionUtils.GetStoreVersion(manifestToken);
        }

        public EFIngresStoreVersion Version { get { return _version; } }

        private static XmlReader GetProviderManifest()
        {
            return GetXmlResource("EFIngresProvider.Resources.EFIngresProviderServices.ProviderManifest.xml");
        }

        private XmlReader GetStoreSchemaMapping()
        {
            return GetXmlResource("EFIngresProvider.Resources.EFIngresProviderServices.StoreSchemaMapping.msl");
        }

        private XmlReader GetStoreSchemaDescription()
        {
            return GetXmlResource("EFIngresProvider.Resources.EFIngresProviderServices.StoreSchemaDefinition.ssdl");
        }

        private static XmlReader GetXmlResource(string resourceName)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Stream stream = executingAssembly.GetManifestResourceStream(resourceName);
            return XmlReader.Create(stream);
        }

        /// <summary>
        /// Providers should override this to return information specific to their provider.  
        /// 
        /// This method should never return null.
        /// </summary>
        /// <param name="informationType">The name of the information to be retrieved.</param>
        /// <returns>An XmlReader at the begining of the information requested.</returns>
        protected override XmlReader GetDbInformation(string informationType)
        {
            if (informationType == DbProviderManifest.StoreSchemaDefinition)
            {
                return GetStoreSchemaDescription();
            }
            if (informationType == DbProviderManifest.StoreSchemaMapping)
            {
                return GetStoreSchemaMapping();
            }

            throw new ProviderIncompatibleException(string.Format("The provider returned null for the informationType '{0}'.", informationType));
        }

        public override ReadOnlyCollection<PrimitiveType> GetStoreTypes()
        {
            if (this._primitiveTypes == null)
            {
                this._primitiveTypes = base.GetStoreTypes();
            }
            return this._primitiveTypes;
        }

        public override ReadOnlyCollection<EdmFunction> GetStoreFunctions()
        {
            if (this._functions == null)
            {
                this._functions = base.GetStoreFunctions();
            }
            return this._functions;
        }

        /// <summary>
        /// This method takes a type and a set of facets and returns the best mapped equivalent type 
        /// in EDM.
        /// </summary>
        /// <param name="storeType">A TypeUsage encapsulating a store type and a set of facets</param>
        /// <returns>A TypeUsage encapsulating an EDM type and a set of facets</returns>
        public override TypeUsage GetEdmType(TypeUsage storeType)
        {
            if (storeType == null)
            {
                throw new ArgumentNullException("storeType");
            }

            string storeTypeName = storeType.EdmType.Name.ToLowerInvariant();
            if (!base.StoreTypeNameToEdmPrimitiveType.ContainsKey(storeTypeName))
            {
                throw new ArgumentException(String.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
            }

            PrimitiveType edmPrimitiveType = base.StoreTypeNameToEdmPrimitiveType[storeTypeName];

            switch (storeTypeName)
            {
                // for some types we just go with simple type usage with no facets
                case "tinyint":
                case "smallint":
                case "integer":
                case "bigint":
                case "integer1":
                case "integer2":
                case "integer4":
                case "integer8":
                case "bool":
                case "int1":
                case "int2":
                case "int4":
                case "int8":
                case "float":
                case "float4":
                case "float8":
                case "double":
                case "real":
                    return TypeUsage.CreateDefaultTypeUsage(edmPrimitiveType);

                case "decimal":
                    return CreateDecimalTypeUsage(edmPrimitiveType, TypeHelpers.GetPrecision(storeType), TypeHelpers.GetScale(storeType));

                case "money":
                    return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType, 14, 2);

                case "varchar":
                    return CreateStringTypeUsage(edmPrimitiveType, false, false, TypeHelpers.GetMaxLength(storeType));

                case "char":
                    return CreateStringTypeUsage(edmPrimitiveType, false, true, TypeHelpers.GetMaxLength(storeType));

                case "nvarchar":
                    return CreateStringTypeUsage(edmPrimitiveType, true, false, TypeHelpers.GetMaxLength(storeType));

                case "nchar":
                    return CreateStringTypeUsage(edmPrimitiveType, true, true, TypeHelpers.GetMaxLength(storeType));

                case "long varchar":
                    return CreateStringTypeUsage(edmPrimitiveType, false, false);

                case "byte":
                    return CreateBinaryTypeUsage(edmPrimitiveType, true, TypeHelpers.GetMaxLength(storeType));

                case "byte varying":
                    return CreateBinaryTypeUsage(edmPrimitiveType, false, TypeHelpers.GetMaxLength(storeType));

                case "long byte":
                    return CreateBinaryTypeUsage(edmPrimitiveType, false);

                case "timestamp":
                case "rowversion":
                    return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, true, 8);

                case "date":
                case "ingresdate":
                case "ansidate":
                    return TypeUsage.CreateDateTimeTypeUsage(edmPrimitiveType, null);

                default:
                    throw new NotSupportedException(String.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
            }
        }

        private TypeUsage CreateDecimalTypeUsage(PrimitiveType primitiveType, byte? precision = null, byte? scale = null)
        {
            if ((precision != null) && (scale != null))
            {
                return TypeUsage.CreateDecimalTypeUsage(primitiveType, precision.Value, scale.Value);
            }
            return TypeUsage.CreateDecimalTypeUsage(primitiveType);
        }

        private TypeUsage CreateStringTypeUsage(PrimitiveType edmPrimitiveType, bool isUnicode, bool isFixedLen, int? maxLength = null)
        {
            if (maxLength == null)
            {
                return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen);
            }
            return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen, maxLength.Value);
        }

        private TypeUsage CreateBinaryTypeUsage(PrimitiveType edmPrimitiveType, bool isFixedLen, int? maxLength = null)
        {
            if (maxLength == null)
            {
                return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen);
            }
            return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen, maxLength.Value);
        }

        /// <summary>
        /// This method takes a type and a set of facets and returns the best mapped equivalent type 
        /// in Ingres, taking the store version into consideration.
        /// </summary>
        /// <param name="storeType">A TypeUsage encapsulating an EDM type and a set of facets</param>
        /// <returns>A TypeUsage encapsulating a store type and a set of facets</returns>
        public override TypeUsage GetStoreType(TypeUsage edmType)
        {
            if (edmType == null)
            {
                throw new ArgumentNullException("edmType");
            }
            Debug.Assert(edmType.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType);

            var primitiveType = edmType.EdmType as PrimitiveType;
            if (primitiveType == null)
            {
                throw new ArgumentException(String.Format("The underlying provider does not support the type '{0}'.", edmType));
            }

            ReadOnlyMetadataCollection<Facet> facets = edmType.Facets;

            switch (primitiveType.PrimitiveTypeKind)
            {
                case PrimitiveTypeKind.Boolean:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["tinyint"]);

                case PrimitiveTypeKind.SByte:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["tinyint"]);

                case PrimitiveTypeKind.Byte:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["smallint"]);

                case PrimitiveTypeKind.Int16:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["smallint"]);

                case PrimitiveTypeKind.Int32:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["integer"]);

                case PrimitiveTypeKind.Int64:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["bigint"]);

                case PrimitiveTypeKind.Double:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["float"]);

                case PrimitiveTypeKind.Single:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["real"]);

                case PrimitiveTypeKind.Decimal: // decimal, money
                    return TypeUsage.CreateDecimalTypeUsage(StoreTypeNameToStorePrimitiveType["decimal"], TypeHelpers.GetPrecision(edmType, 18), TypeHelpers.GetScale(edmType, 0));

                case PrimitiveTypeKind.Binary: // byte, byte varying, long byte
                    {
                        var isFixedLength = facets["FixedLength"].GetValue<bool>(false);
                        var maxLengthFacet = facets["MaxLength"];
                        var maxLength = maxLengthFacet.GetValue<int?>();
                        if (maxLengthFacet.IsUnbounded || maxLength == null || maxLength.Value > binaryMaxSize)
                        {
                            maxLength = null;
                        }

                        var storeTypeName = isFixedLength ? "byte" : maxLength == null ? "long byte" : "byte varying";

                        return CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType[storeTypeName], isFixedLength, maxLength);
                    }

                case PrimitiveTypeKind.String: // char, nchar, varchar, nvarchar, long varchar, long nvarchar
                    {
                        var isUnicode = facets["Unicode"].GetValue<bool>(false);
                        var isFixedLength = facets["FixedLength"].GetValue<bool>(false);
                        var maxLengthFacet = facets["MaxLength"];
                        var maxLength = maxLengthFacet.GetValue<int?>();
                        if (maxLengthFacet.IsUnbounded || maxLength == null || maxLength.Value > varcharMaxSize)
                        {
                            maxLength = null;
                        }

                        var storeTypeName = isFixedLength ? "char" : "varchar";
                        storeTypeName = isUnicode ? "n" + storeTypeName : storeTypeName;
                        storeTypeName = (maxLength == null) ? "long " + storeTypeName : storeTypeName;

                        return CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType[storeTypeName], isUnicode, isFixedLength, maxLength);
                    }

                case PrimitiveTypeKind.DateTime: // ingresdate
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["ingresdate"]);

                case PrimitiveTypeKind.Time: // time
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["time"]);

                default:
                    throw new NotSupportedException(String.Format("There is no store type corresponding to the EDM type '{0}' of primitive type '{1}'.", edmType, primitiveType.PrimitiveTypeKind));
            }
        }

        /// <summary>
        /// Function to detect wildcard characters '%' and '_' and escape them with a preceding \
        /// This escaping is used when StartsWith, EndsWith and Contains canonical and CLR functions
        /// are translated to their equivalent LIKE expression
        /// </summary>
        /// <param name="text">Original input as specified by the user</param>
        /// <param name="alwaysEscapeEscapeChar">escape the escape character \ regardless whether wildcard 
        /// characters were encountered </param>
        /// <param name="usedEscapeChar">true if the escaping was performed, false if no escaping was required</param>
        /// <returns>The escaped string that can be used as pattern in a LIKE expression</returns>
        internal static string EscapeLikeText(string text, bool alwaysEscapeEscapeChar, out bool usedEscapeChar)
        {
            usedEscapeChar = false;
            if (!(text.Contains("%") || text.Contains("_") || alwaysEscapeEscapeChar && text.Contains(LikeEscapeCharString)))
            {
                return text;
            }

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '%' || c == '_' || c == LikeEscapeChar)
                {
                    sb.Append(LikeEscapeChar);
                    usedEscapeChar = true;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        internal const string LikeEscapeCharString = @"\";
        internal const char LikeEscapeChar = '\\';
    }


    #region Helpers

    internal static class TypeHelpers
    {
        public static T GetValue<T>(this Facet facet, T defaultValue = default(T))
        {
            if (facet.Value != null)
            {
                return (T)facet.Value;
            }
            return defaultValue;
        }

        public static byte GetPrecision(TypeUsage tu, byte defaultPrecision)
        {
            return GetPrecision(tu, (byte?)defaultPrecision).Value;
        }

        public static byte? GetPrecision(TypeUsage tu, byte? defaultPrecision = null)
        {
            byte precision;
            if (TryGetPrecision(tu, out precision))
            {
                return precision;
            }
            return defaultPrecision;
        }

        public static bool TryGetPrecision(TypeUsage tu, out byte precision)
        {
            Facet f;

            precision = 0;
            if (tu.Facets.TryGetValue("Precision", false, out f))
            {
                if (!f.IsUnbounded && f.Value != null)
                {
                    precision = (byte)f.Value;
                    return true;
                }
            }
            return false;
        }

        public static int? GetMaxLength(TypeUsage tu)
        {
            int maxLength;
            if (TryGetMaxLength(tu, out maxLength))
            {
                return maxLength;
            }
            return default(int?);
        }

        public static bool TryGetMaxLength(TypeUsage tu, out int maxLength)
        {
            Facet f;

            maxLength = 0;
            if (tu.Facets.TryGetValue("MaxLength", false, out f))
            {
                if (!f.IsUnbounded && f.Value != null)
                {
                    maxLength = (int)f.Value;
                    return true;
                }
            }
            return false;
        }

        public static byte GetScale(TypeUsage tu, byte defaultScale)
        {
            return GetScale(tu, (byte?)defaultScale).Value;
        }

        public static byte? GetScale(TypeUsage tu, byte? defaultScale = null)
        {
            byte scale;
            if (TryGetScale(tu, out scale))
            {
                return scale;
            }
            return defaultScale;
        }

        public static bool TryGetScale(TypeUsage tu, out byte scale)
        {
            Facet f;

            scale = 0;
            if (tu.Facets.TryGetValue("Scale", false, out f))
            {
                if (!f.IsUnbounded && f.Value != null)
                {
                    scale = (byte)f.Value;
                    return true;
                }
            }
            return false;
        }
    }

    #endregion


}
