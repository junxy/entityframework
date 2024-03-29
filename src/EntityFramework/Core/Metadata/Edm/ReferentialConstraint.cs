// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;

    /// <summary>
    ///     This class describes referential constraint on the relationships
    /// </summary>
    public sealed class ReferentialConstraint : MetadataItem
    {
        #region Constructors

        /// <summary>
        ///     Constructs a new constraint on the relationship
        /// </summary>
        /// <param name="fromRole"> role from which the relationship originates </param>
        /// <param name="toRole"> role to which the relationship is linked/targeted to </param>
        /// <param name="toProperties"> properties on entity type of from role which take part in the constraint </param>
        /// <param name="fromProperties"> properties on entity type of to role which take part in the constraint </param>
        /// <exception cref="ArgumentNullException">Argument Null exception if any of the arguments is null</exception>
        internal ReferentialConstraint(
            RelationshipEndMember fromRole,
            RelationshipEndMember toRole,
            IEnumerable<EdmProperty> fromProperties,
            IEnumerable<EdmProperty> toProperties)
        {
            _fromRole = EntityUtil.GenericCheckArgumentNull(fromRole, "fromRole");
            _toRole = EntityUtil.GenericCheckArgumentNull(toRole, "toRole");
            _fromProperties = new ReadOnlyMetadataCollection<EdmProperty>(
                new MetadataCollection<EdmProperty>(
                    EntityUtil.GenericCheckArgumentNull(fromProperties, "fromProperties")));
            _toProperties = new ReadOnlyMetadataCollection<EdmProperty>(
                new MetadataCollection<EdmProperty>(
                    EntityUtil.GenericCheckArgumentNull(toProperties, "toProperties")));
        }

        #endregion

        #region Fields

        private readonly RelationshipEndMember _fromRole;
        private readonly RelationshipEndMember _toRole;
        private readonly ReadOnlyMetadataCollection<EdmProperty> _fromProperties;
        private readonly ReadOnlyMetadataCollection<EdmProperty> _toProperties;

        #endregion

        #region Properties

        /// <summary>
        ///     Returns the kind of the type
        /// </summary>
        public override BuiltInTypeKind BuiltInTypeKind
        {
            get { return BuiltInTypeKind.ReferentialConstraint; }
        }

        /// <summary>
        ///     Returns the identity for this constraint
        /// </summary>
        internal override string Identity
        {
            get { return FromRole.Name + "_" + ToRole.Name; }
        }

        /// <summary>
        ///     Returns the FromRole which takes part in this referential constraint
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown if value passed into setter is null</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the ReferentialConstraint instance is in ReadOnly state</exception>
        [MetadataProperty(BuiltInTypeKind.RelationshipEndMember, false)]
        public RelationshipEndMember FromRole
        {
            get { return _fromRole; }
        }

        /// <summary>
        ///     Returns the ToRole which takes part in this referential constraint
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown if value passed into setter is null</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the ReferentialConstraint instance is in ReadOnly state</exception>
        [MetadataProperty(BuiltInTypeKind.RelationshipEndMember, false)]
        public RelationshipEndMember ToRole
        {
            get { return _toRole; }
        }

        /// <summary>
        ///     Returns the collection of properties on the from role on which the constraint is defined on
        /// </summary>
        [MetadataProperty(BuiltInTypeKind.EdmProperty, true)]
        public ReadOnlyMetadataCollection<EdmProperty> FromProperties
        {
            get { return _fromProperties; }
        }

        /// <summary>
        ///     Returns the collection of properties on the ToRole on whose value the constraint is defined on
        /// </summary>
        [MetadataProperty(BuiltInTypeKind.EdmProperty, true)]
        public ReadOnlyMetadataCollection<EdmProperty> ToProperties
        {
            get { return _toProperties; }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Overriding System.Object.ToString to provide better String representation 
        ///     for this type.
        /// </summary>
        public override string ToString()
        {
            return FromRole.Name + "_" + ToRole.Name;
        }

        /// <summary>
        ///     Sets this item to be read-only, once this is set, the item will never be writable again.
        /// </summary>
        internal override void SetReadOnly()
        {
            if (!IsReadOnly)
            {
                base.SetReadOnly();

                var fromRole = FromRole;
                if (fromRole != null)
                {
                    fromRole.SetReadOnly();
                }

                var toRole = ToRole;
                if (toRole != null)
                {
                    toRole.SetReadOnly();
                }
                FromProperties.Source.SetReadOnly();
                ToProperties.Source.SetReadOnly();
            }
        }

        #endregion
    }
}
