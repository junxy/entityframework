// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    ///     Helper Class for EDM Metadata - this class contains all the helper methods
    ///     which needs access to internal methods. The other partial class contains all 
    ///     helper methods which just uses public methods/properties. The reason why we 
    ///     did this for allowing view gen to happen at compile time - all the helper
    ///     methods that view gen or mapping uses are in the other helper class. Rest of the
    ///     methods are in this class
    /// </summary>
    internal static partial class Helper
    {
        #region Fields

        // List of all the static empty list used all over the code
        internal static readonly ReadOnlyCollection<KeyValuePair<string, object>> EmptyKeyValueStringObjectList =
            new ReadOnlyCollection<KeyValuePair<string, object>>(new KeyValuePair<string, object>[0]);

        internal static readonly ReadOnlyCollection<string> EmptyStringList = new ReadOnlyCollection<string>(new string[0]);

        internal static readonly ReadOnlyCollection<FacetDescription> EmptyFacetDescriptionEnumerable =
            new ReadOnlyCollection<FacetDescription>(new FacetDescription[0]);

        internal static readonly ReadOnlyCollection<EdmFunction> EmptyEdmFunctionReadOnlyCollection =
            new ReadOnlyCollection<EdmFunction>(new EdmFunction[0]);

        internal static readonly ReadOnlyCollection<PrimitiveType> EmptyPrimitiveTypeReadOnlyCollection =
            new ReadOnlyCollection<PrimitiveType>(new PrimitiveType[0]);

        internal static readonly KeyValuePair<string, object>[] EmptyKeyValueStringObjectArray = new KeyValuePair<string, object>[0];

        internal const char PeriodSymbol = '.';
        internal const char CommaSymbol = ',';

        #endregion

        #region Methods

        /// <summary>
        ///     Returns the single error message from the list of errors
        /// </summary>
        /// <param name="errors"> </param>
        /// <returns> </returns>
        internal static string CombineErrorMessage(IEnumerable<EdmSchemaError> errors)
        {
            Debug.Assert(errors != null);
            var sb = new StringBuilder(Environment.NewLine);
            var count = 0;
            foreach (var error in errors)
            {
                //Don't append a new line at the beginning of the messages
                if ((count++) != 0)
                {
                    sb.Append(Environment.NewLine);
                }
                sb.Append(error);
            }
            Debug.Assert(count != 0, "Empty Error List");
            return sb.ToString();
        }

        /// <summary>
        ///     Returns the single error message from the list of errors
        /// </summary>
        /// <param name="errors"> </param>
        /// <returns> </returns>
        internal static string CombineErrorMessage(IEnumerable<EdmItemError> errors)
        {
            var sb = new StringBuilder(Environment.NewLine);
            var count = 0;
            foreach (var error in errors)
            {
                // Only add the new line if this is not the first error
                if ((count++) != 0)
                {
                    sb.Append(Environment.NewLine);
                }
                sb.Append(error.Message);
            }

            return sb.ToString();
        }

        // requires: enumerations must have the same number of members
        // effects: returns paired enumeration values
        internal static IEnumerable<KeyValuePair<T, S>> PairEnumerations<T, S>(IBaseList<T> left, IEnumerable<S> right)
        {
            var leftEnumerator = left.GetEnumerator();
            var rightEnumerator = right.GetEnumerator();

            while (leftEnumerator.MoveNext()
                   && rightEnumerator.MoveNext())
            {
                yield return new KeyValuePair<T, S>((T)leftEnumerator.Current, rightEnumerator.Current);
            }

            yield break;
        }

        /// <summary>
        ///     Returns a model (C-Space) typeusage for the given typeusage. if the type is already in c-space, it returns
        ///     the given typeusage. The typeUsage returned is created by invoking the provider service to map from provider
        ///     specific type to model type.
        /// </summary>
        /// <param name="typeUsage"> typeusage </param>
        /// <returns> the respective Model (C-Space) typeusage </returns>
        internal static TypeUsage GetModelTypeUsage(TypeUsage typeUsage)
        {
            return typeUsage.GetModelTypeUsage();
        }

        /// <summary>
        ///     Returns a model (C-Space) typeusage for the given member typeusage. if the type is already in c-space, it returns
        ///     the given typeusage. The typeUsage returned is created by invoking the provider service to map from provider
        ///     specific type to model type.
        /// </summary>
        /// <param name="member"> EdmMember </param>
        /// <returns> the respective Model (C-Space) typeusage </returns>
        internal static TypeUsage GetModelTypeUsage(EdmMember member)
        {
            return GetModelTypeUsage(member.TypeUsage);
        }

        /// <summary>
        ///     Checks if the edm type in the cspace type usage maps to some sspace type (called it S1). If S1 is equivalent or
        ///     promotable to the store type in sspace type usage, then it creates a new type usage with S1 and copies all facets
        ///     if necessary
        /// </summary>
        /// <param name="edmProperty"> Edm property containing the cspace member type information </param>
        /// <param name="columnProperty"> edm property containing the sspace member type information </param>
        /// <param name="fileName"> name of the mapping file from which this information was loaded from </param>
        /// <returns> </returns>
        internal static TypeUsage ValidateAndConvertTypeUsage(
            EdmProperty edmProperty,
            EdmProperty columnProperty)
        {
            Debug.Assert(edmProperty.TypeUsage.EdmType.DataSpace == DataSpace.CSpace, "cspace property must have a cspace type");
            Debug.Assert(columnProperty.TypeUsage.EdmType.DataSpace == DataSpace.SSpace, "sspace type usage must have a sspace type");
            Debug.Assert(
                IsScalarType(edmProperty.TypeUsage.EdmType),
                "cspace property must be of a primitive or enumeration type");
            Debug.Assert(IsPrimitiveType(columnProperty.TypeUsage.EdmType), "sspace property must contain a primitive type");

            var mappedStoreType = ValidateAndConvertTypeUsage(
                edmProperty.TypeUsage,
                columnProperty.TypeUsage);

            return mappedStoreType;
        }

        internal static TypeUsage ValidateAndConvertTypeUsage(
            TypeUsage cspaceType,
            TypeUsage sspaceType)
        {
            // if we are already C-Space, dont call the provider. this can happen for functions.
            var modelEquivalentSspace = sspaceType;
            if (sspaceType.EdmType.DataSpace
                == DataSpace.SSpace)
            {
                modelEquivalentSspace = sspaceType.GetModelTypeUsage();
            }

            // check that cspace type is subtype of c-space equivalent type from the ssdl definition
            if (ValidateScalarTypesAreCompatible(cspaceType, modelEquivalentSspace))
            {
                return modelEquivalentSspace;
            }
            return null;
        }

        #endregion

        /// <summary>
        ///     Validates whether cspace and sspace types are compatible.
        /// </summary>
        /// <param name="cspaceType"> Type in C-Space. Must be a primitive or enumeration type. </param>
        /// <param name="storeType"> C-Space equivalent of S-space Type. Must be a primitive type. </param>
        /// <returns> <c>true</c> if the types are compatible. <c>false</c> otherwise. </returns>
        /// <remarks>
        ///     This methods validate whether cspace and sspace types are compatible. The types are
        ///     compatible if:
        ///     both are primitive and the cspace type is a subtype of sspace type 
        ///     or
        ///     cspace type is an enumeration type whose underlying type is a subtype of sspace type.
        /// </remarks>
        private static bool ValidateScalarTypesAreCompatible(TypeUsage cspaceType, TypeUsage storeType)
        {
            Debug.Assert(cspaceType != null, "cspaceType != null");
            Debug.Assert(storeType != null, "storeType != null");
            Debug.Assert(cspaceType.EdmType.DataSpace == DataSpace.CSpace, "cspace property must have a cspace type");
            Debug.Assert(storeType.EdmType.DataSpace == DataSpace.CSpace, "storeType type usage must have a sspace type");
            Debug.Assert(
                IsScalarType(cspaceType.EdmType),
                "cspace property must be of a primitive or enumeration type");
            Debug.Assert(IsPrimitiveType(storeType.EdmType), "storeType property must be a primitive type");

            if (IsEnumType(cspaceType.EdmType))
            {
                // For enum cspace type check whether its underlying type is a subtype of the store type. Note that
                // TypeSemantics.IsSubTypeOf uses only TypeUsage.EdmType for primitive types so there is no need to copy facets 
                // from the enum type property to the underlying type TypeUsage created here since they wouldn't be used anyways.
                return TypeSemantics.IsSubTypeOf(TypeUsage.Create(GetUnderlyingEdmTypeForEnumType(cspaceType.EdmType)), storeType);
            }

            return TypeSemantics.IsSubTypeOf(cspaceType, storeType);
        }
    }
}
