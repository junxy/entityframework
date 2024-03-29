// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.DataClasses
{
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects.Internal;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Resources;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Collection of entities modeling a particular EDM construct
    ///     which can either be all entities of a particular type or
    ///     entities participating in a particular relationship.
    /// </summary>
    [Serializable]
    public class EntityCollection<TEntity> : RelatedEnd, ICollection<TEntity>, IListSource
        where TEntity : class
    {
        // ------
        // Fields
        // ------
        // The following field is serialized.  Adding or removing a serialized field is considered
        // a breaking change.  This includes changing the field type or field name of existing
        // serialized fields. If you need to make this kind of change, it may be possible, but it
        // will require some custom serialization/deserialization code.
        // Note that this field should no longer be used directly.  Instead, use the _wrappedRelatedEntities
        // field.  This field is retained only for compatibility with the serialization format introduced in v1.
        private HashSet<TEntity> _relatedEntities;

        [NonSerialized]
        private CollectionChangeEventHandler _onAssociationChangedforObjectView;

        [NonSerialized]
        private Dictionary<TEntity, IEntityWrapper> _wrappedRelatedEntities;

        // ------------
        // Constructors
        // ------------

        /// <summary>
        ///     Creates an empty EntityCollection.
        /// </summary>
        public EntityCollection()
        {
        }

        internal EntityCollection(IEntityWrapper wrappedOwner, RelationshipNavigation navigation, IRelationshipFixer relationshipFixer)
            : base(wrappedOwner, navigation, relationshipFixer)
        {
        }

        // ---------
        // Events
        // ---------

        /// <summary>
        ///     internal Event to notify changes in the collection.
        /// </summary>
        // Dev notes -2
        // following statement is valid on current existing CLR: 
        // lets say Customer is an Entity, Array[Customer] is not Array[Entity]; it is not supported
        // to do the work around we have to use a non-Generic interface/class so we can pass the EntityCollection<T>
        // around safely (as RelatedEnd) without losing it.
        // Dev notes -3 
        // this event is only used for internal purposes, to make sure views are updated before we fire public AssociationChanged event
        internal override event CollectionChangeEventHandler AssociationChangedForObjectView
        {
            add { _onAssociationChangedforObjectView += value; }
            remove { _onAssociationChangedforObjectView -= value; }
        }

        // ---------
        // Properties
        // ---------
        private Dictionary<TEntity, IEntityWrapper> WrappedRelatedEntities
        {
            get
            {
                if (null == _wrappedRelatedEntities)
                {
                    _wrappedRelatedEntities = new Dictionary<TEntity, IEntityWrapper>();
                }
                return _wrappedRelatedEntities;
            }
        }

        // ----------------------
        // ICollection Properties
        // ----------------------

        /// <summary>
        ///     Count of entities in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                DeferredLoad();
                return CountInternal;
            }
        }

        internal int CountInternal
        {
            get
            {
                // count should not cause allocation
                return ((null != _wrappedRelatedEntities) ? _wrappedRelatedEntities.Count : 0);
            }
        }

        /// <summary>
        ///     Whether or not the collection is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        // ----------------------
        // IListSource  Properties
        // ----------------------
        /// <summary>
        ///     IListSource.ContainsListCollection implementation. Always returns false.
        ///     This means that the IList we return is the one which contains our actual data,
        ///     it is not a list of collections.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        bool IListSource.ContainsListCollection
        {
            get { return false; }
        }

        // -------
        // Methods
        // -------

        internal override void OnAssociationChanged(CollectionChangeAction collectionChangeAction, object entity)
        {
            Contract.Assert(!(entity is IEntityWrapper), "Object is an IEntityWrapper instance instead of the raw entity.");
            if (!_suppressEvents)
            {
                if (_onAssociationChangedforObjectView != null)
                {
                    _onAssociationChangedforObjectView(this, (new CollectionChangeEventArgs(collectionChangeAction, entity)));
                }
                if (_onAssociationChanged != null)
                {
                    _onAssociationChanged(this, (new CollectionChangeEventArgs(collectionChangeAction, entity)));
                }
            }
        }

        // ----------------------
        // IListSource  method
        // ----------------------
        /// <summary>
        ///     IListSource.GetList implementation
        /// </summary>
        /// <returns> IList interface over the data to bind </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        IList IListSource.GetList()
        {
            EntityType rootEntityType = null;
            if (WrappedOwner.Entity != null)
            {
                EntitySet singleEntitySet = null;

                // if the collection is attached, we can use metadata information; otherwise, it is unavailable
                if (null != RelationshipSet)
                {
                    singleEntitySet = ((AssociationSet)RelationshipSet).AssociationSetEnds[ToEndMember.Name].EntitySet;
                    var associationEndType = (EntityType)((RefType)(ToEndMember).TypeUsage.EdmType).ElementType;
                    var entitySetType = singleEntitySet.ElementType;

                    // the type is constrained to be either the entitySet.ElementType or the end member type, whichever is most derived
                    if (associationEndType.IsAssignableFrom(entitySetType))
                    {
                        // entity set exposes a subtype of the association
                        rootEntityType = entitySetType;
                    }
                    else
                    {
                        // use the end type otherwise
                        rootEntityType = associationEndType;
                    }
                }
            }

            return ObjectViewFactory.CreateViewForEntityCollection(rootEntityType, this);
        }

        /// <inheritdoc />
        public override void Load(MergeOption mergeOption)
        {
            CheckOwnerNull();

            //Pass in null to indicate the CreateSourceQuery method should be used.
            Load(null, mergeOption);
            // do not fire the AssociationChanged event here,
            // once it is fired in one level deeper, (at Internal void Load(IEnumerable<T>)), you don't need to add the event at other
            // API that call (Internal void Load(IEnumerable<T>))
        }

        /// <inheritdoc />
        public override Task LoadAsync(MergeOption mergeOption, CancellationToken cancellationToken)
        {
            CheckOwnerNull();

            //Pass in null to indicate the CreateSourceQuery method should be used.
            return LoadAsync(null, mergeOption, cancellationToken);
            // do not fire the AssociationChanged event here,
            // once it is fired in one level deeper, (at Internal void Load(IEnumerable<T>)), you don't need to add the event at other
            // API that call (Internal void Load(IEnumerable<T>))
        }

        /// <summary>
        ///     Loads related entities into the local collection. If the collection is already filled
        ///     or partially filled, merges existing entities with the given entities. The given
        ///     entities are not assumed to be the complete set of related entities.
        /// 
        ///     Owner and all entities passed in must be in Unchanged or Modified state. We allow 
        ///     deleted elements only when the state manager is already tracking the relationship
        ///     instance.
        /// </summary>
        /// <param name="entities"> Result of query returning related entities </param>
        /// <exception cref="ArgumentNullException">Thrown when
        ///     <paramref name="entities" />
        ///     is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an entity in the given
        ///     collection cannot be related via the current relationship end.</exception>
        public void Attach(IEnumerable<TEntity> entities)
        {
            Contract.Requires(entities != null);
            CheckOwnerNull();
            IList<IEntityWrapper> wrappedEntities = new List<IEntityWrapper>();
            foreach (var entity in entities)
            {
                wrappedEntities.Add(EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext));
            }
            Attach(wrappedEntities, true);
        }

        /// <summary>
        ///     Attaches an entity to the EntityCollection. If the EntityCollection is already filled
        ///     or partially filled, this merges the existing entities with the given entity. The given
        ///     entity is not assumed to be the complete set of related entities.
        /// 
        ///     Owner and all entities passed in must be in Unchanged or Modified state. 
        ///     Deleted elements are allowed only when the state manager is already tracking the relationship
        ///     instance.
        /// </summary>
        /// <param name="entity"> The entity to attach to the EntityCollection </param>
        /// <exception cref="ArgumentNullException">Thrown when
        ///     <paramref name="entity" />
        ///     is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the entity cannot be related via the current relationship end.</exception>
        public void Attach(TEntity entity)
        {
            Contract.Requires(entity != null);
            Attach(new[] { EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext) }, false);
        }

        /// <summary>
        ///     Requires: collection is null or contains related entities.
        ///     Loads related entities into the local collection.
        /// </summary>
        /// <param name="collection"> If null, retrieves entities from the server through a query; otherwise, loads the given collection </param>
        internal virtual void Load(List<IEntityWrapper> collection, MergeOption mergeOption)
        {
            // Validate that the Load is possible
            bool hasResults;
            var sourceQuery = ValidateLoad<TEntity>(mergeOption, "EntityCollection", out hasResults);

            // we do not want any Add or Remove event to be fired during Merge, we will fire a Refresh event at the end if everything is successful
            _suppressEvents = true;
            try
            {
                if (collection == null)
                {
                    IEnumerable<TEntity> refreshedValues;
                    if (hasResults)
                    {
                        refreshedValues = sourceQuery.Execute(sourceQuery.MergeOption);
                    }
                    else
                    {
                        refreshedValues = Enumerable.Empty<TEntity>();
                    }

                    Merge(refreshedValues, mergeOption, true /*setIsLoaded*/);
                }
                else
                {
                    Merge<TEntity>(collection, mergeOption, true /*setIsLoaded*/);
                }
            }
            finally
            {
                _suppressEvents = false;
            }
            // fire the AssociationChange with Refresh
            OnAssociationChanged(CollectionChangeAction.Refresh, null);
        }

        internal virtual async Task LoadAsync(List<IEntityWrapper> collection, MergeOption mergeOption, CancellationToken cancellationToken)
        {
            // Validate that the Load is possible
            bool hasResults;
            var sourceQuery = ValidateLoad<TEntity>(mergeOption, "EntityCollection", out hasResults);

            // we do not want any Add or Remove event to be fired during Merge, we will fire a Refresh event at the end if everything is successful
            _suppressEvents = true;
            try
            {
                if (collection == null)
                {
                    IEnumerable<TEntity> refreshedValues;
                    if (hasResults)
                    {
                        var queryResult = await sourceQuery.ExecuteAsync(sourceQuery.MergeOption, cancellationToken);
                        refreshedValues = await queryResult.ToListAsync(cancellationToken);
                    }
                    else
                    {
                        refreshedValues = Enumerable.Empty<TEntity>();
                    }

                    Merge(refreshedValues, mergeOption, true /*setIsLoaded*/);
                }
                else
                {
                    Merge<TEntity>(collection, mergeOption, true /*setIsLoaded*/);
                }
            }
            finally
            {
                _suppressEvents = false;
            }
            // fire the AssociationChange with Refresh
            OnAssociationChanged(CollectionChangeAction.Refresh, null);
        }

        public void Add(TEntity item)
        {
            DbHelpers.ThrowIfNull(item, "item");

            Add(EntityWrapperFactory.WrapEntityUsingContext(item, ObjectContext));
        }

        /// <summary>
        ///     Add the item to the underlying collection
        /// </summary>
        /// <param name="entity"> </param>
        internal override void DisconnectedAdd(IEntityWrapper wrappedEntity)
        {
            // Validate that the incoming entity is also detached
            if (null != wrappedEntity.Context
                && wrappedEntity.MergeOption != MergeOption.NoTracking)
            {
                throw new InvalidOperationException(Strings.RelatedEnd_UnableToAddEntity);
            }

            VerifyType(wrappedEntity);

            // Add the entity to local collection without doing any fixup
            AddToCache(wrappedEntity, /* applyConstraints */ false);
            OnAssociationChanged(CollectionChangeAction.Add, wrappedEntity.Entity);
        }

        /// <summary>
        ///     Remove the item from the underlying collection
        /// </summary>
        /// <param name="entity"> </param>
        /// <param name="applyConstraints"> </param>
        internal override bool DisconnectedRemove(IEntityWrapper wrappedEntity)
        {
            // Validate that the incoming entity is also detached
            if (null != wrappedEntity.Context
                && wrappedEntity.MergeOption != MergeOption.NoTracking)
            {
                throw new InvalidOperationException(Strings.RelatedEnd_UnableToRemoveEntity);
            }

            // Remove the entity to local collection without doing any fixup
            var result = RemoveFromCache(wrappedEntity, /* resetIsLoaded*/ false, /*preserveForeignKey*/ false);
            OnAssociationChanged(CollectionChangeAction.Remove, wrappedEntity.Entity);
            return result;
        }

        /// <summary>
        ///     Removes an entity from the EntityCollection.  If the owner is
        ///     attached to a context, Remove marks the relationship for deletion and if
        ///     the relationship is composition also marks the entity for deletion.
        /// </summary>
        /// <param name="entity"> Entity instance to remove from the EntityCollection </param>
        /// <returns> Returns true if the entity was successfully removed, false if the entity was not part of the RelatedEnd. </returns>
        public bool Remove(TEntity item)
        {
            DbHelpers.ThrowIfNull(item, "item");

            DeferredLoad();
            return RemoveInternal(item);
        }

        internal bool RemoveInternal(TEntity entity)
        {
            return Remove(EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext), /*preserveForeignKey*/false);
        }

        internal override void Include(bool addRelationshipAsUnchanged, bool doAttach)
        {
            if (null != _wrappedRelatedEntities
                && null != ObjectContext)
            {
                var wrappedRelatedEntities = new List<IEntityWrapper>(_wrappedRelatedEntities.Values);
                foreach (var wrappedEntity in wrappedRelatedEntities)
                {
                    // Sometimes with mixed POCO and IPOCO, you can get different instances of IEntityWrappers stored in the IPOCO related ends
                    // These should be replaced by the IEntityWrapper that is stored in the context
                    var identityWrapper = EntityWrapperFactory.WrapEntityUsingContext(wrappedEntity.Entity, WrappedOwner.Context);
                    if (identityWrapper != wrappedEntity)
                    {
                        _wrappedRelatedEntities[(TEntity)identityWrapper.Entity] = identityWrapper;
                    }
                    IncludeEntity(identityWrapper, addRelationshipAsUnchanged, doAttach);
                }
            }
        }

        internal override void Exclude()
        {
            if (null != _wrappedRelatedEntities
                && null != ObjectContext)
            {
                if (!IsForeignKey)
                {
                    foreach (var wrappedEntity in _wrappedRelatedEntities.Values)
                    {
                        ExcludeEntity(wrappedEntity);
                    }
                }
                else
                {
                    var tm = ObjectContext.ObjectStateManager.TransactionManager;
                    Contract.Assert(
                        tm.IsAddTracking || tm.IsAttachTracking,
                        "Exclude being called while not part of attach/add rollback--PromotedEntityKeyRefs will be null.");
                    var values = new List<IEntityWrapper>(_wrappedRelatedEntities.Values);
                    foreach (var wrappedEntity in values)
                    {
                        var otherEnd = GetOtherEndOfRelationship(wrappedEntity) as EntityReference;
                        Contract.Assert(otherEnd != null, "Other end of FK from a collection should be a reference.");
                        var doFullRemove = tm.PopulatedEntityReferences.Contains(otherEnd);
                        var doRelatedEndRemove = tm.AlignedEntityReferences.Contains(otherEnd);
                        if (doFullRemove || doRelatedEndRemove)
                        {
                            // Remove the related ends and mark the relationship as deleted, but don't propagate the changes to the target entity itself
                            otherEnd.Remove(
                                otherEnd.CachedValue,
                                doFixup: doFullRemove,
                                deleteEntity: false,
                                deleteOwner: false,
                                applyReferentialConstraints: false,
                                preserveForeignKey: true);
                            // Since this has been processed, remove it from the list
                            if (doFullRemove)
                            {
                                tm.PopulatedEntityReferences.Remove(otherEnd);
                            }
                            else
                            {
                                tm.AlignedEntityReferences.Remove(otherEnd);
                            }
                        }
                        else
                        {
                            ExcludeEntity(wrappedEntity);
                        }
                    }
                }
            }
        }

        internal override void ClearCollectionOrRef(IEntityWrapper wrappedEntity, RelationshipNavigation navigation, bool doCascadeDelete)
        {
            if (null != _wrappedRelatedEntities)
            {
                //copy into list because changing collection member is not allowed during enumeration.
                // If possible avoid copying into list.
                var tempCopy = new List<IEntityWrapper>(_wrappedRelatedEntities.Values);
                foreach (var wrappedCurrent in tempCopy)
                {
                    // Following condition checks if we have already visited this graph node. If its true then
                    // we should not do fixup because that would cause circular loop
                    if ((wrappedEntity.Entity == wrappedCurrent.Entity)
                        && (navigation.Equals(RelationshipNavigation)))
                    {
                        Remove(
                            wrappedCurrent, /*fixup*/false, /*deleteEntity*/false, /*deleteOwner*/false, /*applyReferentialConstraints*/
                            false, /*preserveForeignKey*/false);
                    }
                    else
                    {
                        Remove(
                            wrappedCurrent, /*fixup*/true, doCascadeDelete, /*deleteOwner*/false, /*applyReferentialConstraints*/false,
                            /*preserveForeignKey*/false);
                    }
                }
                Contract.Assert(
                    _wrappedRelatedEntities.Count == 0, "After removing all related entities local collection count should be zero");
            }
        }

        internal override void ClearWrappedValues()
        {
            if (_wrappedRelatedEntities != null)
            {
                _wrappedRelatedEntities.Clear();
            }
            if (_relatedEntities != null)
            {
                _relatedEntities.Clear();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="entity"> </param>
        /// <param name="relationshipAlreadyExists"> </param>
        /// <returns> True if the verify succeeded, False if the Add should no-op </returns>
        internal override bool VerifyEntityForAdd(IEntityWrapper wrappedEntity, bool relationshipAlreadyExists)
        {
            if (!relationshipAlreadyExists
                && ContainsEntity(wrappedEntity))
            {
                return false;
            }

            VerifyType(wrappedEntity);

            return true;
        }

        internal override bool CanSetEntityType(IEntityWrapper wrappedEntity)
        {
            return wrappedEntity.Entity is TEntity;
        }

        internal override void VerifyType(IEntityWrapper wrappedEntity)
        {
            if (!CanSetEntityType(wrappedEntity))
            {
                throw new InvalidOperationException(
                    Strings.RelatedEnd_InvalidContainedType_Collection(wrappedEntity.Entity.GetType().FullName, typeof(TEntity).FullName));
            }
        }

        /// <summary>
        ///     Remove from the RelatedEnd
        /// </summary>
        /// <param name="wrappedEntity"> </param>
        /// <param name="resetIsLoaded"> </param>
        /// <returns> </returns>
        internal override bool RemoveFromLocalCache(IEntityWrapper wrappedEntity, bool resetIsLoaded, bool preserveForeignKey)
        {
            if (_wrappedRelatedEntities != null
                && _wrappedRelatedEntities.Remove((TEntity)wrappedEntity.Entity))
            {
                if (resetIsLoaded)
                {
                    _isLoaded = false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Remove from the POCO collection
        /// </summary>
        /// <param name="wrappedEntity"> </param>
        /// <returns> </returns>
        internal override bool RemoveFromObjectCache(IEntityWrapper wrappedEntity)
        {
            // For POCO entities - remove the object from the CLR collection
            if (TargetAccessor.HasProperty) // Null if the navigation does not exist in this direction
            {
                return WrappedOwner.CollectionRemove(this, wrappedEntity.Entity);
            }

            return false;
        }

        internal override void RetrieveReferentialConstraintProperties(
            Dictionary<string, KeyValuePair<object, IntBox>> properties, HashSet<object> visited)
        {
            // Since there are no RI Constraints which has a collection as a To/Child role,
            // this method is no-op.
        }

        internal override bool IsEmpty()
        {
            return _wrappedRelatedEntities == null || (_wrappedRelatedEntities.Count == 0);
        }

        internal override void VerifyMultiplicityConstraintsForAdd(bool applyConstraints)
        {
            // no-op
        }

        // Update IsLoaded flag if necessary
        // This method is called when Clear() was called on the other end of relationship (if the other end is EntityCollection)
        // or when Value property of the other end was set to null (if the other end is EntityReference).
        // This method is used only when NoTracking option was used.
        internal override void OnRelatedEndClear()
        {
            // If other end of relationship was cleared, it means that this collection is also no longer loaded
            _isLoaded = false;
        }

        internal override bool ContainsEntity(IEntityWrapper wrappedEntity)
        {
            // Using operator 'as' instead of () allows calling ContainsEntity
            // with entity of different type than TEntity.
            var entity = wrappedEntity.Entity as TEntity;
            return _wrappedRelatedEntities == null ? false : _wrappedRelatedEntities.ContainsKey(entity);
        }

        // -------------------
        // ICollection Methods
        // -------------------

        /// <summary>
        ///     Get an enumerator for the collection.
        /// </summary>
        public new IEnumerator<TEntity> GetEnumerator()
        {
            DeferredLoad();
            return WrappedRelatedEntities.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            DeferredLoad();
            return WrappedRelatedEntities.Keys.GetEnumerator();
        }

        internal override IEnumerable GetInternalEnumerable()
        {
            return WrappedRelatedEntities.Keys;
        }

        internal override IEnumerable<IEntityWrapper> GetWrappedEntities()
        {
            return WrappedRelatedEntities.Values;
        }

        /// <summary>
        ///     Removes all entities from the locally cached collection.  Also removes
        ///     relationships related to this entities from the ObjectStateManager.
        /// </summary>
        public void Clear()
        {
            DeferredLoad();
            if (WrappedOwner.Entity != null)
            {
                var shouldFireEvent = (CountInternal > 0);
                if (null != _wrappedRelatedEntities)
                {
                    var affectedEntities = new List<IEntityWrapper>(_wrappedRelatedEntities.Values);

                    try
                    {
                        _suppressEvents = true;

                        foreach (var wrappedEntity in affectedEntities)
                        {
                            // Remove Entity
                            Remove(wrappedEntity, false);

                            if (UsingNoTracking)
                            {
                                // The other end of relationship can be the EntityReference or EntityCollection
                                // If the other end is EntityReference, its IsLoaded property should be set to FALSE
                                var relatedEnd = GetOtherEndOfRelationship(wrappedEntity);
                                relatedEnd.OnRelatedEndClear();
                            }
                        }
                        Contract.Assert(_wrappedRelatedEntities.Count == 0);
                    }
                    finally
                    {
                        _suppressEvents = false;
                    }

                    if (UsingNoTracking)
                    {
                        _isLoaded = false;
                    }
                }

                if (shouldFireEvent)
                {
                    OnAssociationChanged(CollectionChangeAction.Refresh, null);
                }
            }
            else
            {
                // Disconnected Clear should be dispatched to the internal collection
                if (_wrappedRelatedEntities != null)
                {
                    _wrappedRelatedEntities.Clear();
                }
            }
        }

        /// <summary>
        ///     Determine if the collection contains a specific object by reference.
        /// </summary>
        /// <return>true if the collection contains the object by reference;
        ///     otherwise, false</return>
        public bool Contains(TEntity item)
        {
            DeferredLoad();
            return _wrappedRelatedEntities == null ? false : _wrappedRelatedEntities.ContainsKey(item);
        }

        /// <summary>
        ///     Copies the contents of the collection to an array,
        ///     starting at a particular array index.
        /// </summary>
        public void CopyTo(TEntity[] array, int arrayIndex)
        {
            DeferredLoad();
            WrappedRelatedEntities.Keys.CopyTo(array, arrayIndex);
        }

        internal virtual void BulkDeleteAll(List<object> list)
        {
            if (list.Count > 0)
            {
                _suppressEvents = true;
                try
                {
                    foreach (var entity in list)
                    {
                        // Remove Entity
                        RemoveInternal(entity as TEntity);
                    }
                }
                finally
                {
                    _suppressEvents = false;
                }
                OnAssociationChanged(CollectionChangeAction.Refresh, null);
            }
        }

        internal override bool CheckIfNavigationPropertyContainsEntity(IEntityWrapper wrapper)
        {
            Contract.Assert(RelationshipNavigation != null, "null RelationshipNavigation");

            // If the navigation property doesn't exist (e.g. unidirectional prop), then it can't contain the entity.
            if (!TargetAccessor.HasProperty)
            {
                return false;
            }

            var value = WrappedOwner.GetNavigationPropertyValue(this);

            if (value != null)
            {
                var enumerable = value as IEnumerable;
                if (enumerable == null)
                {
                    throw new EntityException(
                        Strings.ObjectStateEntry_UnableToEnumerateCollection(
                            TargetAccessor.PropertyName, WrappedOwner.Entity.GetType().FullName));
                }

                foreach (var o in enumerable)
                {
                    if (Equals(o, wrapper.Entity))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal override void VerifyNavigationPropertyForAdd(IEntityWrapper wrapper)
        {
            // no-op
        }

        // This method is required to maintain compatibility with the v1 binary serialization format. 
        // In particular, it takes the dictionary of wrapped entities and creates a hash set of
        // raw entities that will be serialized.
        // Note that this is only expected to work for non-POCO entities, since serialization of POCO
        // entities will not result in serialization of the RelationshipManager or its related objects.
        [SuppressMessage("Microsoft.Usage", "CA2238:ImplementSerializationMethodsCorrectly")]
        [OnSerializing]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnSerializing(StreamingContext context)
        {
            if (!(WrappedOwner.Entity is IEntityWithRelationships))
            {
                throw new InvalidOperationException(Strings.RelatedEnd_CannotSerialize("EntityCollection"));
            }
            _relatedEntities = _wrappedRelatedEntities == null ? null : new HashSet<TEntity>(_wrappedRelatedEntities.Keys);
        }

        // This method is required to maintain compatibility with the v1 binary serialization format. 
        // In particular, it takes the _relatedEntities HashSet and recreates the dictionary of wrapped
        // entities from it.  This is because the dictionary is not serialized.
        // Note that this is only expected to work for non-POCO entities, since serialization of POCO
        // entities will not result in serialization of the RelationshipManager or its related objects.
        [OnDeserialized]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA2238:ImplementSerializationMethodsCorrectly")]
        public void OnCollectionDeserialized(StreamingContext context)
        {
            if (_relatedEntities != null)
            {
                // We need to call this here so that the hash set will be fully constructed
                // ready for access.  Normally, this would happen later in the process.
                _relatedEntities.OnDeserialization(null);
                _wrappedRelatedEntities = new Dictionary<TEntity, IEntityWrapper>();
                foreach (var entity in _relatedEntities)
                {
                    _wrappedRelatedEntities.Add(entity, EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext));
                }
            }
        }

        // Identical code is in EntityReference, but this can't be moved to the base class because it relies on the
        // knowledge of the generic type, and the base class isn't generic
        public ObjectQuery<TEntity> CreateSourceQuery()
        {
            CheckOwnerNull();
            bool hasResults;
            return CreateSourceQuery<TEntity>(DefaultMergeOption, out hasResults);
        }

        internal override IEnumerable CreateSourceQueryInternal()
        {
            return CreateSourceQuery();
        }

        //End identical code

        #region Add

        internal override void AddToLocalCache(IEntityWrapper wrappedEntity, bool applyConstraints)
        {
            WrappedRelatedEntities[(TEntity)wrappedEntity.Entity] = wrappedEntity;
        }

        internal override void AddToObjectCache(IEntityWrapper wrappedEntity)
        {
            // For POCO entities - add the object to the CLR collection
            if (TargetAccessor.HasProperty) // Null if the navigation does not exist in this direction
            {
                WrappedOwner.CollectionAdd(this, wrappedEntity.Entity);
            }
        }

        #endregion
    }
}
