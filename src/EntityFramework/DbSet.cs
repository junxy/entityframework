﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Internal.Linq;
    using System.Data.Entity.Resources;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     A non-generic version of <see cref="DbSet{T}" /> which can be used when the type of entity
    ///     is not known at build time.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "Name is intentional")]
    [ContractClass(typeof(DbSetContracts))]
    public abstract class DbSet : DbQuery, IInternalSetAdapter
    {
        #region Fields and constructors

        /// <summary>
        ///     Internal constructor prevents external classes deriving from DbSet.
        /// </summary>
        internal DbSet()
        {
        }

        #endregion

        #region Find

        /// <summary>
        ///     Finds an entity with the given primary key values.
        ///     If an entity with the given primary key values exists in the context, then it is
        ///     returned immediately without making a request to the store.  Otherwise, a request
        ///     is made to the store for an entity with the given primary key values and this entity,
        ///     if found, is attached to the context and returned.  If no entity is found in the
        ///     context or the store, then null is returned.
        /// </summary>
        /// <remarks>
        ///     The ordering of composite key values is as defined in the EDM, which is in turn as defined in
        ///     the designer, by the Code First fluent API, or by the DataMember attribute.
        /// </remarks>
        /// <param name="keyValues"> The values of the primary key for the entity to be found. </param>
        /// <returns> The entity found, or null. </returns>
        /// <exception cref="InvalidOperationException">Thrown if multiple entities exist in the context with the primary key values given.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the type of entity is not part of the data model for this context.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the types of the key values do not match the types of the key values for the entity type to be found.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the context has been disposed.</exception>
        public abstract object Find(params object[] keyValues);

        /// <summary>
        ///     An asynchronous version of Find, which
        ///     finds an entity with the given primary key values.
        ///     If an entity with the given primary key values exists in the context, then it is
        ///     returned immediately without making a request to the store.  Otherwise, a request
        ///     is made to the store for an entity with the given primary key values and this entity,
        ///     if found, is attached to the context and returned.  If no entity is found in the
        ///     context or the store, then null is returned.
        /// </summary>
        /// <remarks>
        ///     The ordering of composite key values is as defined in the EDM, which is in turn as defined in
        ///     the designer, by the Code First fluent API, or by the DataMember attribute.
        /// </remarks>
        /// <param name="keyValues"> The values of the primary key for the entity to be found. </param>
        /// <returns> A Task containing the entity found, or null. </returns>
        public Task<object> FindAsync(params object[] keyValues)
        {
            return FindAsync(CancellationToken.None, keyValues);
        }

        /// <summary>
        ///     An asynchronous version of Find, which
        ///     finds an entity with the given primary key values.
        ///     If an entity with the given primary key values exists in the context, then it is
        ///     returned immediately without making a request to the store.  Otherwise, a request
        ///     is made to the store for an entity with the given primary key values and this entity,
        ///     if found, is attached to the context and returned.  If no entity is found in the
        ///     context or the store, then null is returned.
        /// </summary>
        /// <remarks>
        ///     The ordering of composite key values is as defined in the EDM, which is in turn as defined in
        ///     the designer, by the Code First fluent API, or by the DataMember attribute.
        /// </remarks>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. </param>
        /// <param name="keyValues"> The values of the primary key for the entity to be found. </param>
        /// <returns> A Task containing the entity found, or null. </returns>
        public abstract Task<object> FindAsync(CancellationToken cancellationToken, params object[] keyValues);

        #endregion

        #region Data binding/local view

        /// <summary>
        ///     Gets an <see cref="ObservableCollection{T}" /> that represents a local view of all Added, Unchanged,
        ///     and Modified entities in this set.  This local view will stay in sync as entities are added or
        ///     removed from the context.  Likewise, entities added to or removed from the local view will automatically
        ///     be added to or removed from the context.
        /// </summary>
        /// <remarks>
        ///     This property can be used for data binding by populating the set with data, for example by using the Load
        ///     extension method, and then binding to the local data through this property.  For WPF bind to this property
        ///     directly.  For Windows Forms bind to the result of calling ToBindingList on this property
        /// </remarks>
        /// <value> The local view. </value>
        public abstract IList Local { get; }

        #endregion

        #region Attach/Add/Remove

        /// <summary>
        ///     Attaches the given entity to the context underlying the set.  That is, the entity is placed
        ///     into the context in the Unchanged state, just as if it had been read from the database.
        /// </summary>
        /// <param name="entity"> The entity to attach. </param>
        /// <returns> The entity. </returns>
        /// <remarks>
        ///     Attach is used to repopulate a context with an entity that is known to already exist in the database.
        ///     SaveChanges will therefore not attempt to insert an attached entity into the database because
        ///     it is assumed to already be there.
        ///     Note that entities that are already in the context in some other state will have their state set
        ///     to Unchanged.  Attach is a no-op if the entity is already in the context in the Unchanged state.
        /// </remarks>
        public object Attach(object entity)
        {
            Contract.Requires(entity != null);

            InternalSet.Attach(entity);
            return entity;
        }

        /// <summary>
        ///     Adds the given entity to the context underlying the set in the Added state such that it will
        ///     be inserted into the database when SaveChanges is called.
        /// </summary>
        /// <param name="entity"> The entity to add. </param>
        /// <returns> The entity. </returns>
        /// <remarks>
        ///     Note that entities that are already in the context in some other state will have their state set
        ///     to Added.  Add is a no-op if the entity is already in the context in the Added state.
        /// </remarks>
        public object Add(object entity)
        {
            Contract.Requires(entity != null);

            InternalSet.Add(entity);
            return entity;
        }

        /// <summary>
        ///     Marks the given entity as Deleted such that it will be deleted from the database when SaveChanges
        ///     is called.  Note that the entity must exist in the context in some other state before this method
        ///     is called.
        /// </summary>
        /// <param name="entity"> The entity to remove. </param>
        /// <returns> The entity. </returns>
        /// <remarks>
        ///     Note that if the entity exists in the context in the Added state, then this method
        ///     will cause it to be detached from the context.  This is because an Added entity is assumed not to
        ///     exist in the database such that trying to delete it does not make sense.
        /// </remarks>
        public object Remove(object entity)
        {
            Contract.Requires(entity != null);

            InternalSet.Remove(entity);
            return entity;
        }

        #endregion

        #region Create

        /// <summary>
        ///     Creates a new instance of an entity for the type of this set.
        ///     Note that this instance is NOT added or attached to the set.
        ///     The instance returned will be a proxy if the underlying context is configured to create
        ///     proxies and the entity type meets the requirements for creating a proxy.
        /// </summary>
        /// <returns> The entity instance, which may be a proxy. </returns>
        public abstract object Create();

        /// <summary>
        ///     Creates a new instance of an entity for the type of this set or for a type derived
        ///     from the type of this set.
        ///     Note that this instance is NOT added or attached to the set.
        ///     The instance returned will be a proxy if the underlying context is configured to create
        ///     proxies and the entity type meets the requirements for creating a proxy.
        /// </summary>
        /// <returns> The entity instance, which may be a proxy. </returns>
        public abstract object Create(Type derivedEntityType);

        #endregion

        #region Conversion to generic

        /// <summary>
        ///     Returns the equivalent generic <see cref="DbSet{T}" /> object.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity for which the set was created. </typeparam>
        /// <returns> The generic set object. </returns>
        public new DbSet<TEntity> Cast<TEntity>() where TEntity : class
        {
            if (typeof(TEntity)
                != InternalSet.ElementType)
            {
                throw Error.DbEntity_BadTypeForCast(
                    typeof(DbSet).Name, typeof(TEntity).Name, InternalSet.ElementType.Name);
            }

            return (DbSet<TEntity>)InternalSet.InternalContext.Set<TEntity>();
        }

        #endregion

        #region IInternalSetAdapter

        /// <summary>
        ///     The internal IQueryable that is backing this DbQuery
        /// </summary>
        IInternalSet IInternalSetAdapter.InternalSet
        {
            get { return InternalSet; }
        }

        #endregion

        #region InternalSet

        /// <summary>
        ///     Gets the underlying internal set.
        /// </summary>
        /// <value> The internal set. </value>
        internal abstract IInternalSet InternalSet { get; }

        #endregion

        #region SQL queries

        /// <summary>
        ///     Creates a raw SQL query that will return entities in this set.  By default, the
        ///     entities returned are tracked by the context; this can be changed by calling
        ///     AsNoTracking on the <see cref="DbRawSqlQuery" /> returned.
        ///     Note that the entities returned are always of the type for this set and never of
        ///     a derived type.  If the table or tables queried may contain data for other entity
        ///     types, then the SQL query must be written appropriately to ensure that only entities of
        ///     the correct type are returned.
        /// </summary>
        /// <param name="sql"> The SQL query string. </param>
        /// <param name="parameters"> The parameters to apply to the SQL query string. </param>
        /// <returns> A <see cref="DbSqlQuery" /> object that will execute the query when it is enumerated. </returns>
        public DbSqlQuery SqlQuery(string sql, params object[] parameters)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(sql));
            Contract.Requires(parameters != null);

            return new DbSqlQuery(new InternalSqlSetQuery(InternalSet, sql, false, parameters));
        }

        #endregion

        #region Hidden Object methods

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }

        #endregion

        #region Base Member Contracts

        [ContractClassFor(typeof(DbSet))]
        private abstract class DbSetContracts : DbSet
        {
            public override object Create(Type derivedEntityType)
            {
                Contract.Requires(derivedEntityType != null);

                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
