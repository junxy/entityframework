// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    /// <summary>
    ///     Represents the edm member class
    /// </summary>
    public abstract class EdmMember : MetadataItem
    {
        internal EdmMember()
        {
        }

        /// <summary>
        ///     Initializes a new instance of EdmMember class
        /// </summary>
        /// <param name="name"> name of the member </param>
        /// <param name="memberTypeUsage"> type information containing info about member's type and its facet </param>
        internal EdmMember(string name, TypeUsage memberTypeUsage)
        {
            EntityUtil.CheckStringArgument(name, "name");
            EntityUtil.GenericCheckArgumentNull(memberTypeUsage, "memberTypeUsage");
            _name = name;
            _typeUsage = memberTypeUsage;
        }

        private readonly TypeUsage _typeUsage;
        private readonly string _name;
        private StructuralType _declaringType;

        /// <summary>
        ///     Returns the identity of the member
        /// </summary>
        internal override string Identity
        {
            get { return Name; }
        }

        /// <summary>
        ///     Returns the name of the member
        /// </summary>
        [MetadataProperty(PrimitiveTypeKind.String, false)]
        public virtual String Name
        {
            get { return _name; }
        }

        /// <summary>
        ///     Returns the declaring type of the member
        /// </summary>
        public virtual StructuralType DeclaringType
        {
            get { return _declaringType; }
        }

        /// <summary>
        ///     Returns the TypeUsage object containing the type information and facets
        ///     about the type
        /// </summary>
        [MetadataProperty(BuiltInTypeKind.TypeUsage, false)]
        public TypeUsage TypeUsage
        {
            get { return _typeUsage; }
        }

        /// <summary>
        ///     Overriding System.Object.ToString to provide better String representation 
        ///     for this type.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Sets the member to read only mode. Once this is done, there are no changes
        ///     that can be done to this class
        /// </summary>
        internal override void SetReadOnly()
        {
            if (!IsReadOnly)
            {
                base.SetReadOnly();

                // TypeUsage is always readonly, no need to set it
            }
        }

        /// <summary>
        ///     Change the declaring type without doing fixup in the member collection
        /// </summary>
        internal void ChangeDeclaringTypeWithoutCollectionFixup(StructuralType newDeclaringType)
        {
            _declaringType = newDeclaringType;
        }

        /// <summary>
        ///     Tells whether this member is marked as a Computed member in the EDM definition
        /// </summary>
        internal bool IsStoreGeneratedComputed
        {
            get
            {
                Facet item = null;
                if (TypeUsage.Facets.TryGetValue(EdmProviderManifest.StoreGeneratedPatternFacetName, false, out item))
                {
                    return ((StoreGeneratedPattern)item.Value) == StoreGeneratedPattern.Computed;
                }
                return false;
            }
        }

        /// <summary>
        ///     Tells whether this member's Store generated pattern is marked as Identity in the EDM definition
        /// </summary>
        internal bool IsStoreGeneratedIdentity
        {
            get
            {
                Facet item = null;
                if (TypeUsage.Facets.TryGetValue(EdmProviderManifest.StoreGeneratedPatternFacetName, false, out item))
                {
                    return ((StoreGeneratedPattern)item.Value) == StoreGeneratedPattern.Identity;
                }
                return false;
            }
        }
    }
}
