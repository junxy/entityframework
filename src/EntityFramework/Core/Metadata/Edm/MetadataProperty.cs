// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Diagnostics.Contracts;

    /// <summary>
    ///     Class representing a metadata attribute for an item
    /// </summary>
    public class MetadataProperty : MetadataItem
    {
        #region Constructors

        internal MetadataProperty()
        {
        }

        /// <summary>
        ///     The constructor for MetadataProperty taking in a name, a TypeUsage object, and a value for the attribute
        /// </summary>
        /// <param name="name"> The name of this MetadataProperty </param>
        /// <param name="typeUsage"> The TypeUsage describing the type of this MetadataProperty </param>
        /// <param name="value"> The value for this attribute </param>
        /// <exception cref="System.ArgumentNullException">Thrown if typeUsage argument is null</exception>
        internal MetadataProperty(string name, TypeUsage typeUsage, object value)
        {
            EntityUtil.GenericCheckArgumentNull(typeUsage, "typeUsage");

            _name = name;
            _value = value;
            _typeUsage = typeUsage;
            _propertyKind = PropertyKind.Extended;
        }

        /// <summary>
        ///     The constructor for MetadataProperty taking in all the ingredients for creating TypeUsage and the actual value
        /// </summary>
        /// <param name="name"> The name of the attribute </param>
        /// <param name="edmType"> The edm type of the attribute </param>
        /// <param name="isCollectionType"> Whether the collection type of the given edm type should be used </param>
        /// <param name="value"> The value of the attribute </param>
        internal MetadataProperty(string name, EdmType edmType, bool isCollectionType, object value)
        {
            Contract.Requires(edmType != null);

            _name = name;
            _value = value;
            if (isCollectionType)
            {
                _typeUsage = TypeUsage.Create(edmType.GetCollectionType());
            }
            else
            {
                _typeUsage = TypeUsage.Create(edmType);
            }
            _propertyKind = PropertyKind.System;
        }

        #endregion

        #region Fields

        private readonly string _name;
        private readonly PropertyKind _propertyKind;
        private readonly object _value;
        private readonly TypeUsage _typeUsage;

        #endregion

        #region Properties

        /// <summary>
        ///     Returns the kind of the type
        /// </summary>
        public override BuiltInTypeKind BuiltInTypeKind
        {
            get { return BuiltInTypeKind.MetadataProperty; }
        }

        /// <summary>
        ///     Gets the identity of this item
        /// </summary>
        internal override string Identity
        {
            get { return Name; }
        }

        /// <summary>
        ///     Gets/Sets the name of this MetadataProperty
        /// </summary>
        [MetadataProperty(PrimitiveTypeKind.String, false)]
        public virtual string Name
        {
            get
            {
                // The name is immutable, so it should be safe to always get it from the field
                return _name;
            }
        }

        /// <summary>
        ///     Gets/Sets the value of this MetadataProperty
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if the MetadataProperty instance is in readonly state</exception>
        [MetadataProperty(typeof(Object), false)]
        public virtual object Value
        {
            get
            {
                // Check if we're redirecting to an MetadataItem system property
                var redirectValue = _value as MetadataPropertyValue;
                if (null != redirectValue)
                {
                    return redirectValue.GetValue();
                }

                // If not, return the actual stored value
                return _value;
            }
        }

        /// <summary>
        ///     Gets/Sets the TypeUsage object describing the type of this attribute
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown if value passed into setter is null</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the MetadataProperty instance is in readonly state</exception>
        [MetadataProperty(BuiltInTypeKind.TypeUsage, false)]
        public TypeUsage TypeUsage
        {
            get { return _typeUsage; }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Sets this item to be readonly, once this is set, the item will never be writable again.
        /// </summary>
        internal override void SetReadOnly()
        {
            if (!IsReadOnly)
            {
                base.SetReadOnly();

                // TypeUsage is always readonly, no need to set _typeUsage
            }
        }

        /// <summary>
        ///     Returns the kind of the attribute
        /// </summary>
        public PropertyKind PropertyKind
        {
            get { return _propertyKind; }
        }

        #endregion
    }
}
