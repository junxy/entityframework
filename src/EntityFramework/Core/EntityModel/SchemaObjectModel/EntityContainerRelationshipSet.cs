// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityModel.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Diagnostics;
    using System.Xml;

    /// <summary>
    ///     Represents an RelationshipSet element.
    /// </summary>
    internal abstract class EntityContainerRelationshipSet : SchemaElement
    {
        private IRelationship _relationship;
        private string _unresolvedRelationshipTypeName;

        /// <summary>
        ///     Constructs an EntityContainerRelationshipSet
        /// </summary>
        /// <param name="parentElement"> Reference to the schema element. </param>
        public EntityContainerRelationshipSet(EntityContainer parentElement)
            : base(parentElement)
        {
        }

        public override string FQName
        {
            get { return ParentElement.Name + "." + Name; }
        }

        internal IRelationship Relationship
        {
            get { return _relationship; }
            set
            {
                Debug.Assert(value != null, "relationship can never be set to null");
                _relationship = value;
            }
        }

        protected abstract bool HasEnd(string role);
        protected abstract void AddEnd(IRelationshipEnd relationshipEnd, EntityContainerEntitySet entitySet);
        internal abstract IEnumerable<EntityContainerRelationshipSetEnd> Ends { get; }

        /// <summary>
        ///     The method that is called when an Association attribute is encountered.
        /// </summary>
        /// <param name="reader"> An XmlReader positioned at the Association attribute. </param>
        protected void HandleRelationshipTypeNameAttribute(XmlReader reader)
        {
            Debug.Assert(reader != null);
            var value = HandleDottedNameAttribute(reader, _unresolvedRelationshipTypeName);
            if (value.Succeeded)
            {
                _unresolvedRelationshipTypeName = value.Value;
            }
        }

        /// <summary>
        ///     Used during the resolve phase to resolve the type name to the object that represents that type
        /// </summary>
        internal override void ResolveTopLevelNames()
        {
            base.ResolveTopLevelNames();

            if (_relationship == null)
            {
                SchemaType element;
                if (!Schema.ResolveTypeName(this, _unresolvedRelationshipTypeName, out element))
                {
                    return;
                }

                _relationship = element as IRelationship;
                if (_relationship == null)
                {
                    AddError(
                        ErrorCode.InvalidPropertyType, EdmSchemaErrorSeverity.Error,
                        Strings.InvalidRelationshipSetType(element.Name));
                    return;
                }
            }

            foreach (var end in Ends)
            {
                end.ResolveTopLevelNames();
            }
        }

        internal override void ResolveSecondLevelNames()
        {
            base.ResolveSecondLevelNames();
            foreach (var end in Ends)
            {
                end.ResolveSecondLevelNames();
            }
        }

        /// <summary>
        ///     Do all validation for this element here, and delegate to all sub elements
        /// </summary>
        internal override void Validate()
        {
            base.Validate();

            InferEnds();

            // check out the ends
            foreach (var end in Ends)
            {
                end.Validate();
            }

            // Enabling Association between subtypes in case of Referential Constraints, since 
            // CSD is blocked on this. We need to make a long term call about whether we should
            // really allow this. Bug #520216
            //foreach (ReferentialConstraint constraint in Relationship.Constraints)
            //{
            //    IRelationshipEnd dependentEnd = constraint.DependentRole.End;
            //    EntityContainerRelationshipSetEnd setEnd = GetEnd(dependentEnd.Name);
            //    Debug.Assert(setEnd != null);
            //    //Make sure that the EntityType of the dependant role in a referential constraint
            //    //covers the whole EntitySet( i.e. not  a subtype of the EntitySet's type).
            //    if (!setEnd.EntitySet.EntityType.IsOfType(constraint.DependentRole.End.Type))
            //    {
            //        AddError(ErrorCode.InvalidDependentRoleType, EdmSchemaErrorSeverity.Error,
            //            System.Data.Entity.Resources.Strings.InvalidDependentRoleType(dependentEnd.Type.FQName, dependentEnd.Name, 
            //                                dependentEnd.Parent.FQName, setEnd.EntitySet.Name, setEnd.ParentElement.Name));
            //    }
            //}

            // Validate Number of ends is correct
            //    What we know:
            //      No ends are missing, becuase we infered all missing ends
            //      No extra ends are there because the names have been matched, and an extra name will have caused an error
            //
            //    looks like no count validation needs to be done
        }

        /// <summary>
        ///     Adds any ends that need to be infered
        /// </summary>
        private void InferEnds()
        {
            Debug.Assert(Relationship != null);

            foreach (var relationshipEnd in Relationship.Ends)
            {
                if (! HasEnd(relationshipEnd.Name))
                {
                    var entitySet = InferEntitySet(relationshipEnd);
                    if (entitySet != null)
                    {
                        // we don't have this end, we need to add it
                        AddEnd(relationshipEnd, entitySet);
                    }
                }
            }
        }

        /// <summary>
        ///     For the given relationship end, find the EntityContainer Property that will work for the extent
        /// </summary>
        /// <param name="relationshipEnd"> The relationship end of the RelationshipSet that needs and extent </param>
        /// <returns> Null is none could be found, or the EntityContainerProperty that is the valid extent </returns>
        private EntityContainerEntitySet InferEntitySet(IRelationshipEnd relationshipEnd)
        {
            Debug.Assert(relationshipEnd != null, "relationshipEnd parameter is null");

            var possibleExtents = new List<EntityContainerEntitySet>();
            foreach (var set in ParentElement.EntitySets)
            {
                if (relationshipEnd.Type.IsOfType(set.EntityType))
                {
                    possibleExtents.Add(set);
                }
            }

            if (possibleExtents.Count == 1)
            {
                return possibleExtents[0];
            }
            else if (possibleExtents.Count == 0)
            {
                // no matchs
                AddError(
                    ErrorCode.MissingExtentEntityContainerEnd, EdmSchemaErrorSeverity.Error,
                    Strings.MissingEntityContainerEnd(relationshipEnd.Name, FQName));
            }
            else
            {
                // abmigous
                AddError(
                    ErrorCode.AmbiguousEntityContainerEnd, EdmSchemaErrorSeverity.Error,
                    Strings.AmbiguousEntityContainerEnd(relationshipEnd.Name, FQName));
            }

            return null;
        }

        /// <summary>
        ///     The parent element as an EntityContainer
        /// </summary>
        internal new EntityContainer ParentElement
        {
            get { return (EntityContainer)(base.ParentElement); }
        }
    }
}
