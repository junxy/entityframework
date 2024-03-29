// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.DataClasses
{
    using System.Collections;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Represents one end of a relationship.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [ContractClass(typeof(IRelatedEndContracts))]
    public interface IRelatedEnd
    {
        // ----------
        // Properties
        // ----------

        /// <summary>
        ///     IsLoaded returns true if and only if Load was called.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        ///     Name of the relationship in which this IRelatedEnd is participating
        /// </summary>
        string RelationshipName { get; }

        /// <summary>
        ///     Name of the relationship source role used to generate this IRelatedEnd
        /// </summary>
        string SourceRoleName { get; }

        /// <summary>
        ///     Name of the relationship target role used to generate this IRelatedEnd
        /// </summary>
        string TargetRoleName { get; }

        /// <summary>
        ///     The relationship metadata cooresponding to this IRelatedEnd
        /// </summary>
        RelationshipSet RelationshipSet { get; }

        // -------
        // Methods
        // -------

        /// <summary>
        ///     Loads the related entity or entities into the related end using the default merge option.
        /// </summary>
        void Load();

        /// <summary>
        ///     An asynchronous version of Load, which
        ///     loads the related entity or entities into the related end using the default merge option.
        /// </summary>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. </param>
        /// <returns> A Task representing the asynchronous operation. </returns>
        Task LoadAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Loads the related entity or entities into the related end using the specified merge option.
        /// </summary>
        /// <param name="mergeOption"> Merge option to use for loaded entity or entities. </param>
        void Load(MergeOption mergeOption);

        /// <summary>
        ///     An asynchronous version of Load, which
        ///     loads the related entity or entities into the related end using the specified merge option.
        /// </summary>
        /// <param name="mergeOption"> Merge option to use for loaded entity or entities. </param>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. </param>
        /// <returns> A Task representing the asynchronous operation. </returns>
        Task LoadAsync(MergeOption mergeOption, CancellationToken cancellationToken);

        /// <summary>
        ///     Adds an entity to the related end.  If the owner is
        ///     attached to a cache then the all the connected ends are
        ///     added to the object cache and their corresponding relationships 
        ///     are also added to the ObjectStateManager. The RelatedEnd of the
        ///     relationship is also fixed.
        /// </summary>
        /// <param name="entity"> Entity instance to add to the related end </param>
        void Add(IEntityWithRelationships entity);

        /// <summary>
        ///     Adds an entity to the related end.  If the owner is
        ///     attached to a cache then all the connected ends are
        ///     added to the object cache and their corresponding relationships 
        ///     are also added to the ObjectStateManager. The RelatedEnd of the
        ///     relationship is also fixed.
        /// 
        ///     This overload is meant to be used by classes that do not implement IEntityWithRelationships.
        /// </summary>
        /// <param name="entity"> Entity instance to add to the related end </param>
        void Add(object entity);

        /// <summary>
        ///     Removes an entity from the related end.  If owner is
        ///     attached to a cache, marks relationship for deletion and if
        ///     the relationship is composition also marks the entity for deletion.
        /// </summary>
        /// <param name="entity"> Entity instance to remove from the related end </param>
        /// <returns> Returns true if the entity was successfully removed, false if the entity was not part of the IRelatedEnd. </returns>
        bool Remove(IEntityWithRelationships entity);

        /// <summary>
        ///     Removes an entity from the related end.  If owner is
        ///     attached to a cache, marks relationship for deletion and if
        ///     the relationship is composition also marks the entity for deletion.
        /// 
        ///     This overload is meant to be used by classes that do not implement IEntityWithRelationships.
        /// </summary>
        /// <param name="entity"> Entity instance to remove from the related end </param>
        /// <returns> Returns true if the entity was successfully removed, false if the entity was not part of the IRelatedEnd. </returns>
        bool Remove(object entity);

        /// <summary>
        ///     Attaches an entity to the related end. If the related end is already filled
        ///     or partially filled, this merges the existing entities with the given entity. The given
        ///     entity is not assumed to be the complete set of related entities.
        /// 
        ///     Owner and all entities passed in must be in Unchanged or Modified state. 
        ///     Deleted elements are allowed only when the state manager is already tracking the relationship
        ///     instance.
        /// </summary>
        /// <param name="entity"> The entity to attach to the related end </param>
        /// <exception cref="ArgumentNullException">Thrown when
        ///     <paramref name="entity" />
        ///     is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the entity cannot be related via the current relationship end.</exception>
        void Attach(IEntityWithRelationships entity);

        /// <summary>
        ///     Attaches an entity to the related end. If the related end is already filled
        ///     or partially filled, this merges the existing entities with the given entity. The given
        ///     entity is not assumed to be the complete set of related entities.
        /// 
        ///     Owner and all entities passed in must be in Unchanged or Modified state. 
        ///     Deleted elements are allowed only when the state manager is already tracking the relationship
        ///     instance.
        /// 
        ///     This overload is meant to be used by classes that do not implement IEntityWithRelationships.
        /// </summary>
        /// <param name="entity"> The entity to attach to the related end </param>
        /// <exception cref="ArgumentNullException">Thrown when
        ///     <paramref name="entity" />
        ///     is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the entity cannot be related via the current relationship end.</exception>
        void Attach(object entity);

        /// <summary>
        ///     This is the query which represents the source of the
        ///     related end.  It is constructed on demand using the
        ///     _connection and _cache fields and a query string based on
        ///     the type of relationship end and the metadata passed into its
        ///     constructor.
        /// </summary>
        IEnumerable CreateSourceQuery();

        /// <summary>
        ///     Returns an enumerator of all of the values contained within this related end
        /// </summary>
        IEnumerator GetEnumerator();
    }

    #region Interface Member Contracts

    [ContractClassFor(typeof(IRelatedEnd))]
    internal abstract class IRelatedEndContracts : IRelatedEnd
    {
        public bool IsLoaded
        {
            get { throw new NotImplementedException(); }
        }

        public string RelationshipName
        {
            get { throw new NotImplementedException(); }
        }

        public string SourceRoleName
        {
            get { throw new NotImplementedException(); }
        }

        public string TargetRoleName
        {
            get { throw new NotImplementedException(); }
        }

        public RelationshipSet RelationshipSet
        {
            get { throw new NotImplementedException(); }
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void Load(MergeOption mergeOption)
        {
            throw new NotImplementedException();
        }

        public Task LoadAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task LoadAsync(MergeOption mergeOption, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Add(IEntityWithRelationships entity)
        {
            Contract.Requires(entity != null);

            throw new NotImplementedException();
        }

        public void Add(object entity)
        {
            Contract.Requires(entity != null);

            throw new NotImplementedException();
        }

        public bool Remove(IEntityWithRelationships entity)
        {
            Contract.Requires(entity != null);

            throw new NotImplementedException();
        }

        public bool Remove(object entity)
        {
            Contract.Requires(entity != null);

            throw new NotImplementedException();
        }

        public void Attach(IEntityWithRelationships entity)
        {
            Contract.Requires(entity != null);

            throw new NotImplementedException();
        }

        public void Attach(object entity)
        {
            Contract.Requires(entity != null);

            throw new NotImplementedException();
        }

        public IEnumerable CreateSourceQuery()
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
