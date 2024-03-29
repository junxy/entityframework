// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;
    using System.Text;

    /// <summary>
    ///     Mapping metadata for End property of an association.
    /// </summary>
    /// <example>
    ///     For Example if conceptually you could represent the CS MSL file as following
    ///     --Mapping 
    ///     --EntityContainerMapping ( CNorthwind-->SNorthwind )
    ///     --EntitySetMapping
    ///     --EntityTypeMapping
    ///     --MappingFragment
    ///     --EntityKey
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --EntityTypeMapping
    ///     --MappingFragment
    ///     --EntityKey
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --ComplexPropertyMap
    ///     --ComplexTypeMapping
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --ScalarProperyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --DiscriminatorProperyMap ( constant value-->SMemberMetadata )
    ///     --ComplexTypeMapping
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --ScalarProperyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --DiscriminatorProperyMap ( constant value-->SMemberMetadata )
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --AssociationSetMapping 
    ///     --AssociationTypeMapping
    ///     --MappingFragment
    ///     --EndPropertyMap
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --ScalarProperyMap ( CMemberMetadata-->SMemberMetadata )
    ///     --EndPropertyMap
    ///     --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    ///     This class represents the metadata for all the end property map elements in the 
    ///     above example. EndPropertyMaps provide mapping for each end of the association.
    /// </example>
    internal class StorageEndPropertyMapping : StoragePropertyMapping
    {
        #region Constructors

        /// <summary>
        ///     Construct a new End Property mapping object
        /// </summary>
        /// <param name="member"> </param>
        internal StorageEndPropertyMapping(EdmProperty member)
            : base(member)
        {
        }

        #endregion

        #region Fields

        /// <summary>
        ///     List of property mappings that make up the End.
        /// </summary>
        private readonly List<StoragePropertyMapping> m_properties = new List<StoragePropertyMapping>();

        #endregion

        #region Properties

        /// <summary>
        ///     return ReadOnlyCollection of property mappings that are children of this End mapping
        /// </summary>
        internal ReadOnlyCollection<StoragePropertyMapping> Properties
        {
            get { return m_properties.AsReadOnly(); }
        }

        /// <summary>
        ///     The relation end property Metadata object for which the mapping is represented.
        /// </summary>
        internal RelationshipEndMember EndMember { get; set; }

        /// <summary>
        ///     Returns all store properties that are mapped under this mapping fragment
        /// </summary>
        internal IEnumerable<EdmMember> StoreProperties
        {
            get
            {
                return
                    m_properties.OfType<StorageScalarPropertyMapping>().Select((propertyMap => propertyMap.ColumnProperty)).Cast<EdmMember>(
                        
                        );
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Add a property mapping as a child of End property mapping
        /// </summary>
        /// <param name="prop"> </param>
        internal void AddProperty(StoragePropertyMapping prop)
        {
            m_properties.Add(prop);
        }

#if DEBUG
        /// <summary>
        ///     This method is primarily for debugging purposes.
        ///     Will be removed shortly.
        /// </summary>
        /// <param name="index"> </param>
        internal override void Print(int index)
        {
            StorageEntityContainerMapping.GetPrettyPrintString(ref index);
            var sb = new StringBuilder();
            sb.Append("EndPropertyMapping");
            sb.Append("   ");
            if (EndMember != null)
            {
                sb.Append("Name:");
                sb.Append(EndMember.Name);
                sb.Append("   ");
                sb.Append("TypeName:");
                sb.Append(EndMember.TypeUsage.EdmType.FullName);
            }
            sb.Append("   ");
            Console.WriteLine(sb.ToString());
            foreach (var propertyMapping in Properties)
            {
                propertyMapping.Print(index + 5);
            }
        }
#endif

        #endregion
    }
}
