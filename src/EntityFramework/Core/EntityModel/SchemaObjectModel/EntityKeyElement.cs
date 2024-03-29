// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityModel.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Diagnostics;
    using System.Xml;

    /// <summary>
    ///     Represents an Key element in an EntityType element.
    /// </summary>
    internal sealed class EntityKeyElement : SchemaElement
    {
        private List<PropertyRefElement> _keyProperties;

        /// <summary>
        ///     Constructs an EntityContainerAssociationSetEnd
        /// </summary>
        /// <param name="parentElement"> Reference to the schema element. </param>
        public EntityKeyElement(SchemaEntityType parentElement)
            : base(parentElement)
        {
        }

        public IList<PropertyRefElement> KeyProperties
        {
            get
            {
                if (_keyProperties == null)
                {
                    _keyProperties = new List<PropertyRefElement>();
                }
                return _keyProperties;
            }
        }

        protected override bool HandleAttribute(XmlReader reader)
        {
            return false;
        }

        protected override bool HandleElement(XmlReader reader)
        {
            if (base.HandleElement(reader))
            {
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.PropertyRef))
            {
                HandlePropertyRefElement(reader);
                return true;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="reader"> </param>
        private void HandlePropertyRefElement(XmlReader reader)
        {
            var property = new PropertyRefElement(ParentElement);
            property.Parse(reader);
            KeyProperties.Add(property);
        }

        /// <summary>
        ///     Used during the resolve phase to resolve the type name to the object that represents that type
        /// </summary>
        internal override void ResolveTopLevelNames()
        {
            Debug.Assert(_keyProperties != null, "xsd should have verified that there should be atleast one property ref element");
            foreach (var property in _keyProperties)
            {
                if (!property.ResolveNames((SchemaEntityType)ParentElement))
                {
                    AddError(
                        ErrorCode.InvalidKey, EdmSchemaErrorSeverity.Error,
                        Strings.InvalidKeyNoProperty(ParentElement.FQName, property.Name));
                }
            }
        }

        /// <summary>
        ///     Validate all the key properties
        /// </summary>
        internal override void Validate()
        {
            Debug.Assert(_keyProperties != null, "xsd should have verified that there should be atleast one property ref element");
            var propertyLookUp = new Dictionary<string, PropertyRefElement>(StringComparer.Ordinal);

            foreach (var keyProperty in _keyProperties)
            {
                var property = keyProperty.Property;
                Debug.Assert(
                    property != null,
                    "This should never be null, since if we were not able to resolve, we should have never reached to this point");

                if (propertyLookUp.ContainsKey(property.Name))
                {
                    AddError(
                        ErrorCode.DuplicatePropertySpecifiedInEntityKey, EdmSchemaErrorSeverity.Error,
                        Strings.DuplicatePropertyNameSpecifiedInEntityKey(ParentElement.FQName, property.Name));
                    continue;
                }

                propertyLookUp.Add(property.Name, keyProperty);

                if (property.Nullable)
                {
                    AddError(
                        ErrorCode.InvalidKey, EdmSchemaErrorSeverity.Error,
                        Strings.InvalidKeyNullablePart(property.Name, ParentElement.Name));
                }

                // currently we only support key properties of scalar type
                if (!(property.Type is ScalarType || property.Type is SchemaEnumType)
                    || (property.CollectionKind != CollectionKind.None))
                {
                    AddError(
                        ErrorCode.EntityKeyMustBeScalar,
                        EdmSchemaErrorSeverity.Error,
                        Strings.EntityKeyMustBeScalar(property.Name, ParentElement.Name));
                    continue;
                }

                // Enum properties are never backed by binary or spatial type so we can skip the checks here
                if (!(property.Type is SchemaEnumType))
                {
                    Debug.Assert(property.TypeUsage != null, "For scalar type, typeusage must be initialized");

                    var primitivePropertyType = (PrimitiveType)property.TypeUsage.EdmType;
                    if (Schema.DataModel
                        == SchemaDataModelOption.EntityDataModel)
                    {
                        // Binary keys are only supported for V2.0 CSDL, Spatial keys are not supported.
                        if ((primitivePropertyType.PrimitiveTypeKind == PrimitiveTypeKind.Binary
                             && Schema.SchemaVersion < XmlConstants.EdmVersionForV2)
                            ||
                            Helper.IsSpatialType(primitivePropertyType))
                        {
                            AddError(
                                ErrorCode.EntityKeyTypeCurrentlyNotSupported,
                                EdmSchemaErrorSeverity.Error,
                                Strings.EntityKeyTypeCurrentlyNotSupported(
                                    property.Name, ParentElement.FQName, primitivePropertyType.PrimitiveTypeKind));
                        }
                    }
                    else
                    {
                        Debug.Assert(SchemaDataModelOption.ProviderDataModel == Schema.DataModel, "Invalid DataModel encountered");

                        // Binary keys are only supported for V2.0 SSDL, Spatial keys are not supported.
                        if ((primitivePropertyType.PrimitiveTypeKind == PrimitiveTypeKind.Binary
                             && Schema.SchemaVersion < XmlConstants.StoreVersionForV2)
                            ||
                            Helper.IsSpatialType(primitivePropertyType))
                        {
                            AddError(
                                ErrorCode.EntityKeyTypeCurrentlyNotSupported,
                                EdmSchemaErrorSeverity.Error,
                                Strings.EntityKeyTypeCurrentlyNotSupportedInSSDL(
                                    property.Name, ParentElement.FQName,
                                    property.TypeUsage.EdmType.Name, property.TypeUsage.EdmType.BaseType.FullName,
                                    primitivePropertyType.PrimitiveTypeKind));
                        }
                    }
                }
            }
        }
    }
}
