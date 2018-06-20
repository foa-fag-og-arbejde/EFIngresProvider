using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Metadata.Edm;
using System.Diagnostics;
using Ingres.Client;

namespace EFIngresProvider.Helpers
{
    /// <summary>
    /// A set of static helpers for type metadata
    /// </summary>
    public static class MetadataHelpers
    {
        #region Type Helpers

        /// <summary>
        /// Cast the EdmType of the given type usage to the given TEdmType
        /// </summary>
        /// <typeparam name="TEdmType"></typeparam>
        /// <param name="typeUsage"></param>
        /// <returns></returns>
        internal static TEdmType GetEdmType<TEdmType>(TypeUsage typeUsage)
            where TEdmType : EdmType
        {
            return (TEdmType)typeUsage.EdmType;
        }

        /// <summary>
        /// Gets the TypeUsage of the elment if the given type is a collection type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static TypeUsage GetElementTypeUsage(TypeUsage type)
        {
            if (MetadataHelpers.IsCollectionType(type))
            {
                return ((CollectionType)type.EdmType).TypeUsage;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the properties of in the EdmType underlying the input type usage, 
        ///  if that EdmType is a structured type (EntityType, RowType). 
        /// </summary>
        /// <param name="typeUsage"></param>
        /// <returns></returns>
        internal static IList<EdmProperty> GetProperties(TypeUsage typeUsage)
        {
            return MetadataHelpers.GetProperties(typeUsage.EdmType);
        }

        /// <summary>
        /// Retrieves the properties of the given EdmType, if it is
        ///  a structured type (EntityType, RowType). 
        /// </summary>
        /// <param name="edmType"></param>
        /// <returns></returns>
        internal static IList<EdmProperty> GetProperties(EdmType edmType)
        {
            switch (edmType.BuiltInTypeKind)
            {
                case BuiltInTypeKind.ComplexType:
                    return ((ComplexType)edmType).Properties;
                case BuiltInTypeKind.EntityType:
                    return ((EntityType)edmType).Properties;
                case BuiltInTypeKind.RowType:
                    return ((RowType)edmType).Properties;
                default:
                    return new List<EdmProperty>();
            }
        }

        /// <summary>
        /// Is the given type usage over a collection type
        /// </summary>
        /// <param name="typeUsage"></param>
        /// <returns></returns>
        internal static bool IsCollectionType(TypeUsage typeUsage)
        {
            return MetadataHelpers.IsCollectionType(typeUsage.EdmType);
        }

        /// <summary>
        /// Is the given type a collection type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsCollectionType(EdmType type)
        {
            return (type.BuiltInTypeKind == BuiltInTypeKind.CollectionType);
        }

        /// <summary>
        /// Is the given type usage over a primitive type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsPrimitiveType(TypeUsage type)
        {
            return MetadataHelpers.IsPrimitiveType(type.EdmType);
        }

        /// <summary>
        /// Is the given type a primitive type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsPrimitiveType(EdmType type)
        {
            return (BuiltInTypeKind.PrimitiveType == type.BuiltInTypeKind);
        }

        /// <summary>
        /// Is the given type usage over a row type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsRowType(TypeUsage type)
        {
            return MetadataHelpers.IsRowType(type.EdmType);
        }

        /// <summary>
        /// Is the given type usage over an entity type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsEntityType(TypeUsage type)
        {
            return MetadataHelpers.IsEntityType(type.EdmType);
        }

        /// <summary>
        /// Is the given type a row type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsRowType(EdmType type)
        {
            return (BuiltInTypeKind.RowType == type.BuiltInTypeKind);
        }

        /// <summary>
        /// Is the given type an Enity Type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsEntityType(EdmType type)
        {
            return (BuiltInTypeKind.EntityType == type.BuiltInTypeKind);
        }

        /// <summary>
        /// Gets the type of the given type usage if it is a primitive type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeKind"></param>
        /// <returns></returns>
        internal static bool TryGetPrimitiveTypeKind(TypeUsage type, out PrimitiveTypeKind typeKind)
        {
            if (type != null && type.EdmType != null && type.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType)
            {
                typeKind = ((PrimitiveType)type.EdmType).PrimitiveTypeKind;
                return true;
            }

            typeKind = default(PrimitiveTypeKind);
            return false;
        }

        internal static PrimitiveTypeKind GetPrimitiveTypeKind(TypeUsage type)
        {
            PrimitiveTypeKind returnValue;
            if (!MetadataHelpers.TryGetPrimitiveTypeKind(type, out returnValue))
            {
                Debug.Assert(false, "Cannot create parameter of non-primitive type");
                throw new NotSupportedException("Cannot create parameter of non-primitive type");
            }
            return returnValue;
        }

        /// <summary>
        /// Gets the value for the metadata property with the given name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        internal static T TryGetValueForMetadataProperty<T>(MetadataItem item, string propertyName)
        {
            MetadataProperty property;
            if (!item.MetadataProperties.TryGetValue(propertyName, true, out property))
            {
                return default(T);
            }

            return (T)property.Value;
        }

        internal static bool IsPrimitiveType(TypeUsage type, PrimitiveTypeKind primitiveType)
        {
            PrimitiveTypeKind typeKind;
            if (TryGetPrimitiveTypeKind(type, out typeKind))
            {
                return (typeKind == primitiveType);
            }
            return false;
        }

        internal static IngresType GetIngresType(PrimitiveTypeKind primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveTypeKind.Binary: return IngresType.VarBinary;
                case PrimitiveTypeKind.Boolean: return IngresType.TinyInt;
                case PrimitiveTypeKind.Byte: return IngresType.SmallInt;
                case PrimitiveTypeKind.DateTime: return IngresType.DateTime;
                case PrimitiveTypeKind.Decimal: return IngresType.Decimal;
                case PrimitiveTypeKind.Double: return IngresType.Double;
                case PrimitiveTypeKind.Guid: throw new InvalidOperationException("PrimitiveTypeKind Guid is not supported");
                case PrimitiveTypeKind.Single: return IngresType.Real;
                case PrimitiveTypeKind.SByte: return IngresType.TinyInt;
                case PrimitiveTypeKind.Int16: return IngresType.SmallInt;
                case PrimitiveTypeKind.Int32: return IngresType.Int;
                case PrimitiveTypeKind.Int64: return IngresType.BigInt;
                case PrimitiveTypeKind.String: return IngresType.VarChar;
                case PrimitiveTypeKind.Time: return IngresType.Time;
                case PrimitiveTypeKind.DateTimeOffset: throw new InvalidOperationException("PrimitiveTypeKind DateTimeOffset is not supported");
                default:
                    Debug.Fail("unknown PrimitiveTypeKind" + primitiveType.ToString());
                    throw new InvalidOperationException(string.Format("Unknown PrimitiveTypeKind {0}", primitiveType));
            }
        }

        /// <summary>
        /// Returns the sql primitive/native type name. 
        /// It will include size, precision or scale depending on type information present in the 
        /// type facets
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSqlPrimitiveType(TypeUsage type)
        {
            var primitiveType = MetadataHelpers.GetEdmType<PrimitiveType>(type);
            switch (primitiveType.PrimitiveTypeKind)
            {
                case PrimitiveTypeKind.Boolean:
                    return "tinyint";
                case PrimitiveTypeKind.Byte:
                    return "tinyint";
                case PrimitiveTypeKind.SByte:
                    return "smallint";
                case PrimitiveTypeKind.Int16:
                    return "smallint";
                case PrimitiveTypeKind.Int32:
                    return "integer";
                case PrimitiveTypeKind.Int64:
                    return "bigint";
                case PrimitiveTypeKind.Single:
                    return "real";
                case PrimitiveTypeKind.Double:
                    return "float";
                case PrimitiveTypeKind.Time:
                    return "time";
                case PrimitiveTypeKind.DateTime:
                    return "ingresdate";
                case PrimitiveTypeKind.Decimal:
                    var decimalPrecision = MetadataHelpers.GetFacetValueOrDefault<byte>(type, MetadataHelpers.PrecisionFacetName, 18);
                    var decimalScale = MetadataHelpers.GetFacetValueOrDefault<byte>(type, MetadataHelpers.ScaleFacetName, 0);
                    return string.Format("decimal({0}, {1})", decimalPrecision, decimalScale);
                case PrimitiveTypeKind.String:
                    var isUnicode = MetadataHelpers.GetFacetValueOrDefault<bool>(type, MetadataHelpers.UnicodeFacetName, false);
                    var isStringFixedLength = MetadataHelpers.GetFacetValueOrDefault<bool>(type, MetadataHelpers.FixedLengthFacetName, false);
                    var stringLength = MetadataHelpers.GetFacetValueOrDefault<int?>(type, MetadataHelpers.MaxLengthFacetName, 4000);
                    var isStringLong = (stringLength == null) || (isUnicode && !isStringFixedLength && (stringLength > 4000)) || (!isUnicode && !isStringFixedLength && (stringLength > 8000));
                    var stringType = isStringFixedLength && !isStringLong ? "char" : "varchar";
                    if (isUnicode)
                    {
                        stringType = "n" + stringType;
                    }
                    if (isStringLong)
                    {
                        return "long " + stringType;
                    }
                    return string.Format("{0}({1})", stringType, stringLength);
                case PrimitiveTypeKind.Binary:
                    var isBinaryFixedLength = MetadataHelpers.GetFacetValueOrDefault<bool>(type, MetadataHelpers.FixedLengthFacetName, false);
                    var binaryLength = MetadataHelpers.GetFacetValueOrDefault<int?>(type, MetadataHelpers.MaxLengthFacetName, null);
                    var isBinaryLong = (binaryLength == null) || (!isBinaryFixedLength && (binaryLength > 8000));
                    if (isBinaryLong)
                    {
                        return "long byte";
                    }
                    return string.Format("byte{0}({1})", isBinaryFixedLength ? " varying" : "", binaryLength);
                default:
                    throw new NotSupportedException("Unsupported EdmType: " + primitiveType.PrimitiveTypeKind);
            }
        }

        #endregion

        #region Facet Support
        internal static readonly int UnicodeStringMaxMaxLength = Int32.MaxValue / 2;
        internal static readonly int AsciiStringMaxMaxLength = Int32.MaxValue;
        internal static readonly int BinaryMaxMaxLength = Int32.MaxValue;

        #region Facet Names
        /// <summary>
        /// Name of the MaxLength Facet
        /// </summary>
        public static readonly string MaxLengthFacetName = "MaxLength";

        /// <summary>
        /// Name of the Unicode Facet
        /// </summary>
        public static readonly string UnicodeFacetName = "Unicode";

        /// <summary>
        /// Name of the FixedLength Facet
        /// </summary>
        public static readonly string FixedLengthFacetName = "FixedLength";

        /// <summary>
        /// Name of the PreserveSeconds Facet
        /// </summary>
        public static readonly string PreserveSecondsFacetName = "PreserveSeconds";

        /// <summary>
        /// Name of the Precision Facet
        /// </summary>
        public static readonly string PrecisionFacetName = "Precision";

        /// <summary>
        /// Name of the Scale Facet
        /// </summary>
        public static readonly string ScaleFacetName = "Scale";

        /// <summary>
        /// Name of the DefaultValue Facet
        /// </summary>
        public static readonly string DefaultValueFacetName = "DefaultValue";

        /// <summary>
        /// Name of the Nullable Facet
        /// </summary>
        internal const string NullableFacetName = "Nullable";
        #endregion

        #region Facet Retreival Helpers

        /// <summary>
        /// Get the value specified on the given type usage for the given facet name.
        /// If the faces does not have a value specifid or that value is null returns
        /// the default value for that facet.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="facetName"></param>
        /// <returns></returns>
        /// <summary>
        /// Get the value specified on the given type usage for the given facet name.
        /// If the faces does not have a value specifid or that value is null returns
        /// the default value for that facet.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="facetName"></param>
        /// <returns></returns>
        internal static T GetFacetValueOrDefault<T>(TypeUsage type, string facetName, T defaultValue)
        {
            //Get the value for the facet, if any
            Facet facet;
            if (type.Facets.TryGetValue(facetName, false, out facet) && facet.Value != null && !facet.IsUnbounded)
            {
                return (T)facet.Value;
            }
            else
            {
                return defaultValue;
            }
        }

        internal static bool IsFacetValueConstant(TypeUsage type, string facetName)
        {
            return MetadataHelpers.GetFacet(((PrimitiveType)type.EdmType).FacetDescriptions, facetName).IsConstant;
        }

        private static FacetDescription GetFacet(IEnumerable<FacetDescription> facetCollection, string facetName)
        {
            foreach (FacetDescription facetDescription in facetCollection)
            {
                if (facetDescription.FacetName == facetName)
                {
                    return facetDescription;
                }
            }

            return null;
        }

        /// <summary>
        /// Given a facet name and an EdmType, tries to get that facet's description.
        /// </summary>
        /// <param name="edmType"></param>
        /// <param name="facetName"></param>
        /// <param name="facetDescription"></param>
        /// <returns></returns>
        internal static bool TryGetTypeFacetDescriptionByName(EdmType edmType, string facetName, out FacetDescription facetDescription)
        {
            facetDescription = null;
            if (MetadataHelpers.IsPrimitiveType(edmType))
            {
                PrimitiveType primitiveType = (PrimitiveType)edmType;
                foreach (FacetDescription fd in primitiveType.FacetDescriptions)
                {
                    if (facetName.Equals(fd.FacetName, StringComparison.OrdinalIgnoreCase))
                    {
                        facetDescription = fd;
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool IsNullable(TypeUsage type)
        {
            Facet nullableFacet;
            if (type.Facets.TryGetValue(NullableFacetName, false, out nullableFacet))
            {
                return (bool)nullableFacet.Value;
            }
            return false;
        }

        internal static bool TryGetMaxLength(TypeUsage type, out int maxLength)
        {
            if (!IsPrimitiveType(type, PrimitiveTypeKind.String) &&
                !IsPrimitiveType(type, PrimitiveTypeKind.Binary))
            {
                maxLength = 0;
                return false;
            }

            // Binary and String FixedLength facets share the same name
            return TryGetIntFacetValue(type, MaxLengthFacetName, out maxLength);
        }

        internal static bool TryGetIntFacetValue(TypeUsage type, string facetName, out int intValue)
        {
            intValue = 0;
            Facet intFacet;

            if (type.Facets.TryGetValue(facetName, false, out intFacet) && intFacet.Value != null && !intFacet.IsUnbounded)
            {
                intValue = (int)intFacet.Value;
                return true;
            }

            return false;
        }

        internal static bool TryGetIsFixedLength(TypeUsage type, out bool isFixedLength)
        {
            if (!IsPrimitiveType(type, PrimitiveTypeKind.String) &&
                !IsPrimitiveType(type, PrimitiveTypeKind.Binary))
            {
                isFixedLength = false;
                return false;
            }

            // Binary and String MaxLength facets share the same name
            return TryGetBooleanFacetValue(type, FixedLengthFacetName, out isFixedLength);
        }

        internal static bool TryGetBooleanFacetValue(TypeUsage type, string facetName, out bool boolValue)
        {
            boolValue = false;
            Facet boolFacet;
            if (type.Facets.TryGetValue(facetName, false, out boolFacet) && boolFacet.Value != null)
            {
                boolValue = (bool)boolFacet.Value;
                return true;
            }

            return false;
        }

        internal static bool TryGetIsUnicode(TypeUsage type, out bool isUnicode)
        {
            if (!IsPrimitiveType(type, PrimitiveTypeKind.String))
            {
                isUnicode = false;
                return false;
            }

            return TryGetBooleanFacetValue(type, UnicodeFacetName, out isUnicode);
        }

        #endregion

        #endregion


        internal static bool IsCanonicalFunction(EdmFunction function)
        {
            return (function.NamespaceName == "Edm");
        }

        internal static bool IsStoreFunction(EdmFunction function)
        {
            return !IsCanonicalFunction(function);
        }

        // Returns ParameterDirection corresponding to given ParameterMode
        internal static ParameterDirection ParameterModeToParameterDirection(ParameterMode mode)
        {
            switch (mode)
            {
                case ParameterMode.In:
                    return ParameterDirection.Input;
                case ParameterMode.InOut:
                    return ParameterDirection.InputOutput;
                case ParameterMode.Out:
                    return ParameterDirection.Output;
                case ParameterMode.ReturnValue:
                    return ParameterDirection.ReturnValue;
                default:
                    Debug.Fail("unrecognized mode " + mode.ToString());
                    return default(ParameterDirection);
            }
        }
    }
}
