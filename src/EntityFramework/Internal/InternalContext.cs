﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.Entity.Config;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Internal.Linq;
    using System.Data.Entity.Internal.MockingProxies;
    using System.Data.Entity.Internal.Validation;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Migrations.History;
    using System.Data.Entity.Migrations.Infrastructure;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Data.Entity.Validation;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using SaveOptions = System.Data.Entity.Core.Objects.SaveOptions;

    /// <summary>
    ///     An <see cref="InternalContext" /> underlies every instance of <see cref="DbContext" /> and wraps an
    ///     <see cref="ObjectContext" /> instance.
    ///     The <see cref="InternalContext" /> also acts to expose necessary information to other parts of the design in a
    ///     controlled manner without adding a lot of internal methods and properties to the <see cref="DbContext" />
    ///     class itself.
    ///     Two concrete classes derive from this abstract class - <see cref="LazyInternalContext" /> and
    ///     <see cref="EagerInternalContext" />.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [ContractClass(typeof(InternalContextContracts))]
    internal abstract class InternalContext
    {
        #region Fields and constructors

        private static readonly MethodInfo _createObjectAsObjectMethod = typeof(InternalContext).GetMethod(
            "CreateObjectAsObject", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly ConcurrentDictionary<Type, Func<InternalContext, object>> _entityFactories =
            new ConcurrentDictionary<Type, Func<InternalContext, object>>();

        private static readonly MethodInfo _executeSqlQueryAsIEnumeratorMethod =
            typeof(InternalContext).GetMethod(
                "ExecuteSqlQueryAsIEnumerator", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo _executeSqlQueryAsIDbAsyncEnumeratorMethod =
            typeof(InternalContext).GetMethod(
                "ExecuteSqlQueryAsIDbAsyncEnumerator", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly ConcurrentDictionary<Type, Func<InternalContext, string, object[], IEnumerator>>
            _queryExecutors =
                new ConcurrentDictionary<Type, Func<InternalContext, string, object[], IEnumerator>>();

        private static readonly ConcurrentDictionary<Type, Func<InternalContext, string, object[], IDbAsyncEnumerator>>
            _asyncQueryExecutors =
                new ConcurrentDictionary<Type, Func<InternalContext, string, object[], IDbAsyncEnumerator>>();

        private static readonly ConcurrentDictionary<Type, Func<InternalContext, IInternalSet, IInternalSetAdapter>>
            _setFactories =
                new ConcurrentDictionary<Type, Func<InternalContext, IInternalSet, IInternalSetAdapter>>();

        private static readonly MethodInfo _createInitializationAction =
            typeof(InternalContext).GetMethod("CreateInitializationAction", BindingFlags.Instance | BindingFlags.NonPublic);

        // The configuration to use for initializers, connection strings and default connection factory
        private AppConfig _appConfig = AppConfig.DefaultInstance;

        // The DbContext that owns this InternalContext instance
        private readonly DbContext _owner;

        // Cache of the types that are valid mapped types for this context, together with
        // the entity sets to which these types map and the CLR type that acts as the base
        // of the inheritance hierarchy for the given type.
        private IDictionary<Type, EntitySetTypePair> _entitySetMappings;

        // Usually null, but can be set to a temporary ObjectContext that is used for transient operations
        // such as seeding a database and is then disposed.
        private ClonedObjectContext _tempObjectContext;

        // Counts the number of calls to UseTempObjectContext that need to be unwound.
        private int _tempObjectContextCount;

        // Cache of created DbSet<T>/DbSet objects so that DbContext.Set<T>/Set always returns the same instance.
        private readonly Dictionary<Type, IInternalSetAdapter> _genericSets =
            new Dictionary<Type, IInternalSetAdapter>();

        private readonly Dictionary<Type, IInternalSetAdapter> _nonGenericSets =
            new Dictionary<Type, IInternalSetAdapter>();

        // Used to create validators to validate entities or properties and contexts for validating entities and properties.
        private readonly ValidationProvider _validationProvider = new ValidationProvider();

        private bool _oSpaceLoadingForced;

        public event EventHandler<EventArgs> OnDisposing;

        /// <summary>
        ///     Initializes the <see cref="InternalContext" /> object with its <see cref="DbContext" /> owner.
        /// </summary>
        /// <param name="owner"> The owner <see cref="DbContext" /> . </param>
        protected InternalContext(DbContext owner)
        {
            Contract.Requires(owner != null);

            _owner = owner;
            AutoDetectChangesEnabled = true;
            ValidateOnSaveEnabled = true;
        }

        #endregion

        #region Owner access

        /// <summary>
        ///     The public context instance that owns this internal context.
        /// </summary>
        public DbContext Owner
        {
            get { return _owner; }
        }

        #endregion

        #region ObjectContext and model

        /// <summary>
        ///     Returns the underlying <see cref="ObjectContext" />.
        /// </summary>
        public abstract ObjectContext ObjectContext { get; }

        /// <summary>
        ///     Returns the underlying <see cref="ObjectContext" /> without causing the underlying database to be created
        ///     or the database initialization strategy to be executed.
        ///     This is used to get a context that can then be used for database creation/initialization.
        /// </summary>
        public abstract ObjectContext GetObjectContextWithoutDatabaseInitialization();

        /// <summary>
        ///     Returns the underlying <see cref="ObjectContext" /> without causing the underlying database to be created
        ///     or the database initialization strategy to be executed.
        ///     This is used to get a context that can then be used for database creation/initialization.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public virtual ClonedObjectContext CreateObjectContextForDdlOps()
        {
            InitializeContext();

            return new ClonedObjectContext(
                new ObjectContextProxy(GetObjectContextWithoutDatabaseInitialization()), OriginalConnectionString,
                transferLoadedAssemblies: false);
        }

        /// <summary>
        ///     Gets the temp object context, or null if none has been set.
        /// </summary>
        /// <value> The temp object context. </value>
        protected ObjectContext TempObjectContext
        {
            get { return _tempObjectContext == null ? null : _tempObjectContext.ObjectContext; }
        }

        /// <summary>
        ///     Creates a new temporary <see cref="ObjectContext" /> based on the same metadata and connection as the real
        ///     <see cref="ObjectContext" /> and sets it as the context to use DisposeTempObjectContext is called.
        ///     This allows this internal context and its DbContext to be used for transient operations
        ///     such as initializing and seeding the database, after which it can be thrown away.
        ///     This isolates the real <see cref="ObjectContext" /> from any changes made and and saves performed.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public virtual void UseTempObjectContext()
        {
            _tempObjectContextCount++;
            if (_tempObjectContext == null)
            {
                _tempObjectContext =
                    new ClonedObjectContext(
                        new ObjectContextProxy(GetObjectContextWithoutDatabaseInitialization()),
                        OriginalConnectionString);

                InitializeEntitySetMappings();
            }
        }

        /// <summary>
        ///     If a temporary ObjectContext was set with UseTempObjectContext, then this method disposes that context
        ///     and returns this internal context and its DbContext to using the real ObjectContext.
        /// </summary>
        public virtual void DisposeTempObjectContext()
        {
            if (_tempObjectContextCount > 0)
            {
                if (--_tempObjectContextCount == 0
                    && _tempObjectContext != null)
                {
                    _tempObjectContext.Dispose();
                    _tempObjectContext = null;
                    InitializeEntitySetMappings();
                }
            }
        }

        /// <summary>
        ///     The compiled model created from the Code First pipeline, or null if Code First was
        ///     not used to create this context.
        ///     Causes the Code First pipeline to be run to create the model if it has not already been
        ///     created.
        /// </summary>
        public virtual DbCompiledModel CodeFirstModel
        {
            get { return null; }
        }

        /// <summary>
        ///     Called by methods of <see cref="Database" /> to create a database either using the Migrations pipeline
        ///     if possible and the core provider otherwise.
        /// </summary>
        /// <param name="objectContext"> The context to use for core provider calls. </param>
        public virtual void CreateDatabase(ObjectContext objectContext)
        {
            // objectContext may be null when testing.
            new DatabaseCreator().CreateDatabase(
                this, (config, context) => new DbMigrator(config, context), objectContext);
        }

        /// <summary>
        ///     Internal implementation of <see cref="Database.CompatibleWithModel(bool)" />.
        /// </summary>
        /// <returns> True if the model hash in the context and the database match; false otherwise. </returns>
        public virtual bool CompatibleWithModel(bool throwIfNoMetadata)
        {
            return new ModelCompatibilityChecker().CompatibleWithModel(
                this, new ModelHashCalculator(), throwIfNoMetadata);
        }

        /// <summary>
        ///     Checks whether the given model (an EDMX document) matches the current model.
        /// </summary>
        public virtual bool ModelMatches(XDocument model)
        {
            Contract.Requires(model != null);

            return !new EdmModelDiffer().Diff(model, Owner.GetModel()).Any();
        }

        /// <summary>
        ///     Queries the database for a model hash and returns it if found or returns null if the table
        ///     or the row doesn't exist in the database.
        /// </summary>
        /// <returns> The model hash, or null if not found. </returns>
        public virtual string QueryForModelHash()
        {
            return new EdmMetadataRepository(
                OriginalConnectionString, DbProviderServices.GetProviderFactory(Connection))
                .QueryForModelHash(c => new EdmMetadataContext(c));
        }

        /// <summary>
        ///     Queries the database for a model stored in the MigrationHistory table and returns it as an EDMX, or returns
        ///     null if the database does not contain a model.
        /// </summary>
        public virtual XDocument QueryForModel()
        {
            return new HistoryRepository(OriginalConnectionString, DbProviderServices.GetProviderFactory(Connection))
                .GetLastModel();
        }

        /// <summary>
        ///     Saves the model hash from the context to the database.
        /// </summary>
        public virtual void SaveMetadataToDatabase()
        {
            if (CodeFirstModel != null)
            {
                PerformInitializationAction(
                    () =>
                    new HistoryRepository(OriginalConnectionString, DbProviderServices.GetProviderFactory(Connection))
                        .BootstrapUsingEFProviderDdl(Owner.GetModel()));
            }
        }

        /// <summary>
        ///     Set to true when a database initializer is performing some actions, such as creating or deleting
        ///     a database, or seeding the database.
        /// </summary>
        protected bool InInitializationAction { get; set; }

        /// <summary>
        ///     Performs the initialization action that may result in a <see cref="DbUpdateException" /> and
        ///     handle the exception to provide more meaning to the user.
        /// </summary>
        /// <param name="action"> The action. </param>
        public void PerformInitializationAction(Action action)
        {
            if (InInitializationAction)
            {
                // If this is a nested initialization action, such as creating a database from inside an
                // an initializer, then don't catch and wrap a second time.
                action();
            }
            else
            {
                try
                {
                    InInitializationAction = true;
                    action();
                }
                catch (DataException ex)
                {
                    // For data-related exceptions, wrap the exception into something that lets the user know the context since it
                    // can seem weird to get, for example, an update exception when the user was executing a query.
                    throw new DataException(Strings.Database_InitializationException, ex);
                }
                finally
                {
                    InInitializationAction = false;
                }
            }
        }

        /// <summary>
        ///     Registers for the ObjectStateManagerChanged event on the underlying ObjectStateManager.
        ///     This is a virtual method on this class so that it can be mocked.
        /// </summary>
        /// <param name="handler"> The event handler. </param>
        public virtual void RegisterObjectStateManagerChangedEvent(CollectionChangeEventHandler handler)
        {
            ObjectContext.ObjectStateManager.ObjectStateManagerChanged += handler;
        }

        /// <summary>
        ///     Checks whether or not the given object is in the context in any state other than Deleted.
        ///     This is a virtual method on this class so that it can be mocked.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <returns> <c>true</c> if the entity is in the context and not deleted; otherwise <c>false</c> . </returns>
        public virtual bool EntityInContextAndNotDeleted(object entity)
        {
            ObjectStateEntry stateEntry;
            return ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out stateEntry) &&
                   stateEntry.State != EntityState.Deleted;
        }

        #endregion

        #region SaveChanges

        /// <summary>
        ///     Saves all changes made in this context to the underlying database.
        /// </summary>
        /// <returns> The number of objects written to the underlying database. </returns>
        public virtual int SaveChanges()
        {
            try
            {
                if (ValidateOnSaveEnabled)
                {
                    var validationResults = Owner.GetValidationErrors();
                    if (validationResults.Any())
                    {
                        throw new DbEntityValidationException(
                            Strings.DbEntityValidationException_ValidationFailed, validationResults);
                    }
                }

                var shouldDetectChanges = AutoDetectChangesEnabled && !ValidateOnSaveEnabled;
                var saveOptions = SaveOptions.AcceptAllChangesAfterSave |
                                  (shouldDetectChanges ? SaveOptions.DetectChangesBeforeSave : 0);
                return ObjectContext.SaveChanges(saveOptions);
            }
            catch (UpdateException ex)
            {
                throw WrapUpdateException(ex);
            }
        }

        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (ValidateOnSaveEnabled)
                {
                    var validationResults = Owner.GetValidationErrors();
                    if (validationResults.Any())
                    {
                        throw new DbEntityValidationException(
                            Strings.DbEntityValidationException_ValidationFailed, validationResults);
                    }
                }

                var shouldDetectChanges = AutoDetectChangesEnabled && !ValidateOnSaveEnabled;
                var saveOptions = SaveOptions.AcceptAllChangesAfterSave |
                                  (shouldDetectChanges ? SaveOptions.DetectChangesBeforeSave : 0);
                return await ObjectContext.SaveChangesAsync(saveOptions, cancellationToken);
            }
            catch (UpdateException ex)
            {
                throw WrapUpdateException(ex);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes this instance, which means both the context is initialized and the underlying
        ///     database is initialized.
        /// </summary>
        public void Initialize()
        {
            InitializeContext();
            InitializeDatabase();
        }

        /// <summary>
        ///     Initializes the underlying ObjectContext but does not cause the database to be initialized.
        /// </summary>
        protected abstract void InitializeContext();

        /// <summary>
        ///     Marks the database as having not been initialized. This is called when the app calls Database.Delete so
        ///     that the database if the app attempts to then use the database again it will be re-initialized automatically.
        /// </summary>
        public abstract void MarkDatabaseNotInitialized();

        /// <summary>
        ///     Runs the <see cref="IDatabaseInitializer{TContext}" /> unless it has already been run or there
        ///     is no initializer for this context type in which case this method does nothing.
        /// </summary>
        protected abstract void InitializeDatabase();

        /// <summary>
        ///     Marks the database as having been initialized without actually running the <see cref="IDatabaseInitializer{TContext}" />.
        /// </summary>
        public abstract void MarkDatabaseInitialized();

        /// <summary>
        ///     Runs the <see cref="IDatabaseInitializer{TContext}" /> if one has been set for this context type.
        ///     Calling this method will always cause the initializer to run even if the database is marked
        ///     as initialized.
        /// </summary>
        public void PerformDatabaseInitialization()
        {
            var dbConfiguration = DbConfiguration.Instance;
            var initializer = dbConfiguration.DependencyResolver
                                  .GetService(typeof(IDatabaseInitializer<>).MakeGenericType(Owner.GetType()))
                              ?? DefaultInitializer
                              ?? new NullDatabaseInitializer<DbContext>();

            var initializerAction =
                (Action)_createInitializationAction.MakeGenericMethod(Owner.GetType()).Invoke(this, new[] { initializer });

            try
            {
                UseTempObjectContext();
                PerformInitializationAction(initializerAction);
            }
            finally
            {
                DisposeTempObjectContext();
            }
        }

        private Action CreateInitializationAction<TContext>(IDatabaseInitializer<TContext> initializer)
            where TContext : DbContext
        {
            return () => initializer.InitializeDatabase((TContext)Owner);
        }

        /// <summary>
        ///     Gets the default database initializer to use for this context if no other has been registered.
        ///     For code first this property returns a <see cref="CreateDatabaseIfNotExists{TContext}" /> instance.
        ///     For database/model first, this property returns null.
        /// </summary>
        /// <value> The default initializer. </value>
        public abstract IDatabaseInitializer<DbContext> DefaultInitializer { get; }

        #endregion

        #region Context options

        /// <summary>
        ///     Gets or sets a value indicating whether lazy loading is enabled.
        /// </summary>
        public abstract bool LazyLoadingEnabled { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether proxy creation is enabled.
        /// </summary>
        public abstract bool ProxyCreationEnabled { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether DetectChanges is called automatically in the API.
        /// </summary>
        public bool AutoDetectChangesEnabled { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to validate entities when <see cref="DbContext.SaveChanges()" /> is called.
        /// </summary>
        public bool ValidateOnSaveEnabled { get; set; }

        #endregion

        #region Dispose

        /// <summary>
        ///     Disposes the context. Override the DisposeContext method to perform
        ///     additional work when disposing.
        /// </summary>
        public void Dispose()
        {
            DisposeContext();
            IsDisposed = true;
        }

        /// <summary>
        ///     Performs additional work to dispose a context.
        /// </summary>
        public virtual void DisposeContext()
        {
            if (!IsDisposed
                && OnDisposing != null)
            {
                OnDisposing(this, new EventArgs());
                OnDisposing = null;
            }
        }

        /// <summary>
        ///     True if the context has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion

        #region DetectChanges

        /// <summary>
        ///     Calls DetectChanges on the underlying <see cref="ObjectContext" /> if AutoDetectChangesEnabled is
        ///     true or if force is set to true.
        /// </summary>
        /// <param name="force"> if set to <c>true</c> then DetectChanges is called regardless of the value of AutoDetectChangesEnabled. </param>
        public virtual void DetectChanges(bool force = false)
        {
            if (AutoDetectChangesEnabled || force)
            {
                ObjectContext.DetectChanges();
            }
        }

        #endregion

        #region EntitySet and DbSet access

        /// <summary>
        ///     Returns the DbSet instance for the given entity type.
        ///     This property is virtual and returns <see cref="IDbSet{T}" /> to that it can be mocked.
        /// </summary>
        /// <typeparam name="TEntity"> The entity type for which a set should be returned. </typeparam>
        /// <returns> A set for the given entity type. </returns>
        public virtual IDbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            if (typeof(TEntity)
                != ObjectContextTypeCache.GetObjectType(typeof(TEntity)))
            {
                throw Error.CannotCallGenericSetWithProxyType();
            }

            IInternalSetAdapter set;
            if (!_genericSets.TryGetValue(typeof(TEntity), out set))
            {
                // Check to see if we created the internal set already for a non_generic DbSet wrapper.  If we did,
                // then re-use it.  If not, then create one.
                var internalSet = _nonGenericSets.TryGetValue(typeof(TEntity), out set)
                                      ? set.InternalSet
                                      : new InternalSet<TEntity>(this);
                set = new DbSet<TEntity>((InternalSet<TEntity>)internalSet);
                _genericSets.Add(typeof(TEntity), set);
            }
            return (IDbSet<TEntity>)set;
        }

        /// <summary>
        ///     Returns the non-generic <see cref="DbSet" /> instance for the given entity type.
        ///     This property is virtual and returns <see cref="IInternalSetAdapter" /> to that it can be mocked.
        /// </summary>
        /// <param name="entityType"> The entity type for which a set should be returned. </param>
        /// <returns> A set for the given entity type. </returns>
        public virtual IInternalSetAdapter Set(Type entityType)
        {
            entityType = ObjectContextTypeCache.GetObjectType(entityType);

            IInternalSetAdapter set;
            if (!_nonGenericSets.TryGetValue(entityType, out set))
            {
                // We need to create a non-generic DbSet instance here, which is actually an instance of InternalDbSet<T>.
                // The CreateInternalSet method does this and will wrap the new object either around an existing
                // internal set if one can be found from the generic sets cache, or else will create a new one.
                set = CreateInternalSet(
                    entityType, _genericSets.TryGetValue(entityType, out set) ? set.InternalSet : null);
                _nonGenericSets.Add(entityType, set);
            }
            return set;
        }

        /// <summary>
        ///     Creates an internal set using an app domain cached delegate.
        /// </summary>
        /// <param name="entityType"> Type of the entity. </param>
        /// <returns> The set. </returns>
        private IInternalSetAdapter CreateInternalSet(Type entityType, IInternalSet internalSet)
        {
            Func<InternalContext, IInternalSet, IInternalSetAdapter> factory;
            if (!_setFactories.TryGetValue(entityType, out factory))
            {
                // No value type can ever be an entity type in the model
                if (entityType.IsValueType)
                {
                    throw Error.DbSet_EntityTypeNotInModel(entityType.Name);
                }

                var genericType = typeof(InternalDbSet<>).MakeGenericType(entityType);
                var factoryMethod = genericType.GetMethod(
                    "Create", BindingFlags.Static | BindingFlags.Public, null,
                    new[] { typeof(InternalContext), typeof(IInternalSet) }, null);
                factory =
                    (Func<InternalContext, IInternalSet, IInternalSetAdapter>)
                    Delegate.CreateDelegate(
                        typeof(Func<InternalContext, IInternalSet, IInternalSetAdapter>), factoryMethod);
                _setFactories.TryAdd(entityType, factory);
            }
            return factory(this, internalSet);
        }

        /// <summary>
        ///     Returns the entity set and the base type for that entity set for the given type.
        ///     This method does o-space loading if required and throws if the type is not in the model.
        /// </summary>
        /// <param name="entityType"> The entity type to lookup. </param>
        /// <returns> The entity set and base type pair. </returns>
        public virtual EntitySetTypePair GetEntitySetAndBaseTypeForType(Type entityType)
        {
            Contract.Assert(entityType != null);
            Contract.Assert(
                entityType == ObjectContextTypeCache.GetObjectType(entityType),
                "Proxy type should have been converted to real type");

            Initialize();

            UpdateEntitySetMappingsForType(entityType);
            return _entitySetMappings[entityType];
        }

        /// <summary>
        ///     Returns the entity set and the base type for that entity set for the given type if that
        ///     type is mapped in the model, otherwise returns null.
        ///     This method does o-space loading if required.
        /// </summary>
        /// <param name="entityType"> The entity type to lookup. </param>
        /// <returns> The entity set and base type pair, or null if not found. </returns>
        public virtual EntitySetTypePair TryGetEntitySetAndBaseTypeForType(Type entityType)
        {
            Contract.Assert(entityType != null);
            Contract.Assert(
                entityType == ObjectContextTypeCache.GetObjectType(entityType),
                "Proxy type should have been converted to real type");

            Initialize();

            return TryUpdateEntitySetMappingsForType(entityType) ? _entitySetMappings[entityType] : null;
        }

        /// <summary>
        ///     Checks whether or not the given entity type is mapped in the model.
        /// </summary>
        /// <param name="entityType"> The entity type to lookup. </param>
        /// <returns> True if the type is mapped as an entity; false otherwise. </returns>
        public virtual bool IsEntityTypeMapped(Type entityType)
        {
            Contract.Assert(entityType != null);
            Contract.Assert(
                entityType == ObjectContextTypeCache.GetObjectType(entityType),
                "Proxy type should have been converted to real type");

            Initialize();

            return TryUpdateEntitySetMappingsForType(entityType);
        }

        #endregion

        #region Local data

        /// <summary>
        ///     Gets the local entities of the type specified from the state manager.  That is, all
        ///     Added, Modified, and Unchanged entities of the given type.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity to get. </typeparam>
        /// <returns> The entities. </returns>
        public virtual IEnumerable<TEntity> GetLocalEntities<TEntity>()
        {
            const EntityState StatesToInclude = EntityState.Added | EntityState.Modified | EntityState.Unchanged;

            return
                ObjectContext.ObjectStateManager.GetObjectStateEntries(StatesToInclude).Where(e => e.Entity is TEntity).
                    Select(
                        e => (TEntity)e.Entity);
        }

        #endregion

        #region Raw SQL query

        /// <summary>
        ///     Returns an <see cref="IEnumerator{TElement}" /> which when enumerated will execute the given SQL query against the
        ///     database backing this context. The results are not materialized as entities or tracked.
        /// </summary>
        /// <typeparam name="TElement"> The type of the element. </typeparam>
        /// <param name="sql"> The SQL. </param>
        /// <param name="parameters"> The parameters. </param>
        /// <returns> The query results. </returns>
        public virtual IEnumerator<TElement> ExecuteSqlQuery<TElement>(string sql, object[] parameters)
        {
            Contract.Requires(sql != null);
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<IEnumerator<TElement>>() != null);

            return new LazyEnumerator<TElement>(
                () =>
                    {
                        Initialize();

                        var disposableEnumerable = ObjectContext.ExecuteStoreQuery<TElement>(sql, parameters);
                        try
                        {
                            var result = disposableEnumerable.GetEnumerator();
                            return result;
                        }
                        catch
                        {
                            // if there is a problem creating the enumerator, we should dispose
                            // the enumerable (if there is no problem, the enumerator will take 
                            // care of the dispose)
                            disposableEnumerable.Dispose();
                            throw;
                        }
                    });
        }

        /// <summary>
        ///     Returns an <see cref="IDbAsyncEnumerator{TElement}" /> which when enumerated will execute the given SQL query against the
        ///     database backing this context. The results are not materialized as entities or tracked.
        /// </summary>
        /// <typeparam name="TElement"> The type of the element. </typeparam>
        /// <param name="sql"> The SQL. </param>
        /// <param name="parameters"> The parameters. </param>
        /// <returns> Task containing the query results. </returns>
        public virtual IDbAsyncEnumerator<TElement> ExecuteSqlQueryAsync<TElement>(string sql, object[] parameters)
        {
            Contract.Requires(sql != null);
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<IDbAsyncEnumerator<TElement>>() != null);

            return new LazyAsyncEnumerator<TElement>(
                async () =>
                          {
                              //Not initializing asynchronously as it's not expected to be done frequently
                              Initialize();

                              var disposableEnumerable = await ObjectContext.ExecuteStoreQueryAsync<TElement>(
                                  sql, CancellationToken.None, parameters);

                              try
                              {
                                  return ((IDbAsyncEnumerable<TElement>)disposableEnumerable).GetAsyncEnumerator();
                              }
                              catch
                              {
                                  // if there is a problem creating the enumerator, we should dispose
                                  // the enumerable (if there is no problem, the enumerator will take 
                                  // care of the dispose)
                                  disposableEnumerable.Dispose();
                                  throw;
                              }
                          });
        }

        /// <summary>
        ///     Returns an <see cref="IEnumerator" /> which when enumerated will execute the given SQL query against the
        ///     database backing this context. The results are not materialized as entities or tracked.
        /// </summary>
        /// <param name="elementType"> Type of the element. </param>
        /// <param name="sql"> The SQL. </param>
        /// <param name="parameters"> The parameters. </param>
        /// <returns> The query results. </returns>
        public virtual IEnumerator ExecuteSqlQuery(Type elementType, string sql, object[] parameters)
        {
            // There is no non-generic ExecuteStoreQuery method on ObjectContext so we are
            // forced to use MakeGenericMethod.  We compile this into a delegate so that we
            // only take the hit once.
            Func<InternalContext, string, object[], IEnumerator> executor;
            if (!_queryExecutors.TryGetValue(elementType, out executor))
            {
                var genericExecuteMethod = _executeSqlQueryAsIEnumeratorMethod.MakeGenericMethod(elementType);
                executor =
                    (Func<InternalContext, string, object[], IEnumerator>)
                    Delegate.CreateDelegate(
                        typeof(Func<InternalContext, string, object[], IEnumerator>), genericExecuteMethod);
                _queryExecutors.TryAdd(elementType, executor);
            }
            return executor(this, sql, parameters);
        }

        /// <summary>
        ///     Calls the generic ExecuteSqlQuery but with a non-generic return type so that it
        ///     has the correct signature to be used with CreateDelegate above.
        /// </summary>
        private IEnumerator ExecuteSqlQueryAsIEnumerator<TElement>(string sql, object[] parameters)
        {
            return ExecuteSqlQuery<TElement>(sql, parameters);
        }

        /// <summary>
        ///     Returns an <see cref="IDbAsyncEnumerator" /> which when enumerated will execute the given SQL query against the
        ///     database backing this context. The results are not materialized as entities or tracked.
        /// </summary>
        /// <param name="elementType"> Type of the element. </param>
        /// <param name="sql"> The SQL. </param>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. </param>
        /// <param name="parameters"> The parameters. </param>
        /// <returns> The query results. </returns>
        public virtual IDbAsyncEnumerator ExecuteSqlQueryAsync(Type elementType, string sql, object[] parameters)
        {
            // There is no non-generic ExecuteStoreQuery method on ObjectContext so we are
            // forced to use MakeGenericMethod.  We compile this into a delegate so that we
            // only take the hit once.
            Func<InternalContext, string, object[], IDbAsyncEnumerator> executor;
            if (!_asyncQueryExecutors.TryGetValue(elementType, out executor))
            {
                var genericExecuteMethod = _executeSqlQueryAsIDbAsyncEnumeratorMethod.MakeGenericMethod(elementType);
                executor =
                    (Func<InternalContext, string, object[], IDbAsyncEnumerator>)
                    Delegate.CreateDelegate(
                        typeof(Func<InternalContext, string, object[], IDbAsyncEnumerator>), genericExecuteMethod);
                _asyncQueryExecutors.TryAdd(elementType, executor);
            }
            return executor(this, sql, parameters);
        }

        /// <summary>
        ///     Calls the generic ExecuteSqlQueryAsync but with an object return type so that it
        ///     has the correct signature to be used with CreateDelegate above.
        /// </summary>
        private IDbAsyncEnumerator ExecuteSqlQueryAsIDbAsyncEnumerator<TElement>(string sql, object[] parameters)
            where TElement : class
        {
            return ExecuteSqlQueryAsync<TElement>(sql, parameters);
        }

        /// <summary>
        ///     Executes the given SQL command against the database backing this context.
        /// </summary>
        /// <param name="sql"> The SQL. </param>
        /// <param name="parameters"> The parameters. </param>
        /// <returns> The return value from the database. </returns>
        public virtual int ExecuteSqlCommand(string sql, object[] parameters)
        {
            Contract.Requires(sql != null);
            Contract.Requires(parameters != null);

            Initialize();

            return ObjectContext.ExecuteStoreCommand(sql, parameters);
        }

        /// <summary>
        ///     An asynchronous version of ExecuteSqlCommand, which
        ///     executes the given SQL command against the database backing this context.
        /// </summary>
        /// <param name="sql"> The SQL. </param>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. </param>
        /// <param name="parameters"> The parameters. </param>
        /// <returns> A Task containing the return value from the database. </returns>
        public virtual Task<int> ExecuteSqlCommandAsync(string sql, CancellationToken cancellationToken, object[] parameters)
        {
            Contract.Requires(sql != null);
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<Task<int>>() != null);

            Initialize();

            return ObjectContext.ExecuteStoreCommandAsync(sql, cancellationToken, parameters);
        }

        #endregion

        #region Entity entries

        /// <summary>
        ///     Gets the underlying <see cref="ObjectStateEntry" /> for the given entity, or returns null if the entity isn't tracked by this context.
        ///     This method is virtual so that it can be mocked.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <returns> The state entry or null. </returns>
        public virtual IEntityStateEntry GetStateEntry(object entity)
        {
            Contract.Requires(entity != null);

            DetectChanges();

            ObjectStateEntry entry;
            if (!ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out entry))
            {
                return null;
            }
            return new StateEntryAdapter(entry);
        }

        /// <summary>
        ///     Gets the underlying <see cref="ObjectStateEntry" /> objects for all entities tracked by
        ///     this context.
        ///     This method is virtual so that it can be mocked.
        /// </summary>
        /// <returns> State entries for all tracked entities. </returns>
        public virtual IEnumerable<IEntityStateEntry> GetStateEntries()
        {
            return GetStateEntries(e => e.Entity != null);
        }

        /// <summary>
        ///     Gets the underlying <see cref="ObjectStateEntry" /> objects for all entities of the given
        ///     type tracked by this context.
        ///     This method is virtual so that it can be mocked.
        /// </summary>
        /// <typeparam name="TEntity"> The type of the entity. </typeparam>
        /// <returns> State entries for all tracked entities of the given type. </returns>
        public virtual IEnumerable<IEntityStateEntry> GetStateEntries<TEntity>() where TEntity : class
        {
            return GetStateEntries(e => e.Entity is TEntity);
        }

        /// <summary>
        ///     Helper method that gets the underlying <see cref="ObjectStateEntry" /> objects for all entities that
        ///     match the given predicate.
        /// </summary>
        private IEnumerable<IEntityStateEntry> GetStateEntries(Func<ObjectStateEntry, bool> predicate)
        {
            DetectChanges();

            return
                ObjectContext.ObjectStateManager.GetObjectStateEntries(~EntityState.Detached).Where(predicate).Select(
                    e => new StateEntryAdapter(e));
        }

        /// <summary>
        ///     Wraps the given <see cref="UpdateException" /> in either a <see cref="DbUpdateException" /> or
        ///     a <see cref="DbUpdateConcurrencyException" /> depending on the actual exception type and the state
        ///     entries involved.
        /// </summary>
        /// <param name="updateException"> The update exception. </param>
        /// <returns> A new exception wrapping the given exception. </returns>
        public virtual Exception WrapUpdateException(UpdateException updateException)
        {
            Contract.Requires(updateException != null);
            Contract.Assert(updateException.StateEntries != null);

            if (updateException.StateEntries.Any(e => e.Entity == null))
            {
                // Exception involves a stub or relationship entry => entry involves an independent association.
                return new DbUpdateException(this, updateException, involvesIndependentAssociations: true);
            }

            var asOptimisticConcurrencyException = updateException as OptimisticConcurrencyException;
            return asOptimisticConcurrencyException != null
                       ? new DbUpdateConcurrencyException(this, asOptimisticConcurrencyException)
                       : new DbUpdateException(this, updateException, involvesIndependentAssociations: false);
        }

        #endregion

        #region CreateObject

        /// <summary>
        ///     Uses the underlying context to create an entity such that if the context is configured
        ///     to create proxies and the entity is suitable then a proxy instance will be returned.
        ///     This method is virtual so that it can be mocked.
        /// </summary>
        /// <typeparam name="TEntity"> The type of the entity. </typeparam>
        /// <returns> The new entity instance. </returns>
        public virtual TEntity CreateObject<TEntity>() where TEntity : class
        {
            return ObjectContext.CreateObject<TEntity>();
        }

        /// <summary>
        ///     Uses the underlying context to create an entity such that if the context is configured
        ///     to create proxies and the entity is suitable then a proxy instance will be returned.
        ///     This method is virtual so that it can be mocked.
        /// </summary>
        /// <param name="type"> The type of entity to create. </param>
        /// <returns> The new entity instance. </returns>
        public virtual object CreateObject(Type type)
        {
            Func<InternalContext, object> entityFactory;
            if (!_entityFactories.TryGetValue(type, out entityFactory))
            {
                var factoryMethod = _createObjectAsObjectMethod.MakeGenericMethod(type);
                entityFactory =
                    (Func<InternalContext, object>)
                    Delegate.CreateDelegate(typeof(Func<InternalContext, object>), factoryMethod);
                _entityFactories.TryAdd(type, entityFactory);
            }
            return entityFactory(this);
        }

        /// <summary>
        ///     This method is used by CreateDelegate to transform the CreateObject method with return type TEntity
        ///     into a method with return type object which matches the required type of the delegate.
        /// </summary>
        private object CreateObjectAsObject<TEntity>() where TEntity : class
        {
            return CreateObject<TEntity>();
        }

        #endregion

        #region Connection access and management

        /// <summary>
        ///     The connection underlying this context.  Accessing this property does not cause the context
        ///     to be initialized, only its connection.
        /// </summary>
        public abstract DbConnection Connection { get; }

        /// <summary>
        ///     The connection string as originally applied to the context. This is used to perform operations
        ///     that need the connection string in a non-mutated form, such as with security info still intact.
        /// </summary>
        public abstract string OriginalConnectionString { get; }

        /// <summary>
        ///     Returns the origin of the underlying connection string.
        /// </summary>
        public abstract DbConnectionStringOrigin ConnectionStringOrigin { get; }

        /// <summary>
        ///     Replaces the connection that will be used by this context.
        ///     The connection can only be changed before the context is initialized.
        /// </summary>
        /// <param name="connection"> The new connection. </param>
        public abstract void OverrideConnection(IInternalConnection connection);

        /// <summary>
        ///     Gets or sets an object representing a config file used for looking for DefaultConnectionFactory entries,
        ///     database intializers and connection strings.
        /// </summary>
        public virtual AppConfig AppConfig
        {
            get
            {
                CheckContextNotDisposed();
                return _appConfig;
            }
            set
            {
                CheckContextNotDisposed();
                _appConfig = value;
            }
        }

        /// <summary>
        ///     Gets or sets the provider details to be used when building the EDM model.
        /// </summary>
        public virtual DbProviderInfo ModelProviderInfo
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///     Gets the name of the underlying connection string.
        /// </summary>
        public virtual string ConnectionStringName
        {
            get { return null; }
        }

        /// <summary>
        ///     Gets the provider name bsing used either using a cached value or getting it from
        ///     the DbConnection in use.
        /// </summary>
        public virtual string ProviderName
        {
            get { return Connection.GetProviderInvariantName(); }
        }

        /// <summary>
        ///     Gets or sets a custom OnModelCreating action.
        /// </summary>
        public virtual Action<DbModelBuilder> OnModelCreating
        {
            get { return null; }
            set { }
        }

        #endregion

        #region Database operations

        /// <summary>
        ///     Gets the DatabaseOperations instance to use to perform Create/Delete/Exists operations
        ///     against the database.
        ///     Note that this virtual property can be mocked to help with unit testing.
        /// </summary>
        public virtual DatabaseOperations DatabaseOperations
        {
            get { return new DatabaseOperations(); }
        }

        #endregion

        #region Initialization

        /// <summary>
        ///     Throws if the context has been disposed.
        /// </summary>
        protected void CheckContextNotDisposed()
        {
            if (IsDisposed)
            {
                throw Error.DbContext_Disposed();
            }
        }

        /// <summary>
        ///     Checks whether or not the internal cache of types to entity sets has been initialized,
        ///     and initializes it if necessary.
        /// </summary>
        protected void InitializeEntitySetMappings()
        {
            _entitySetMappings = new Dictionary<Type, EntitySetTypePair>();
            foreach (var set in _genericSets.Values.Union(_nonGenericSets.Values))
            {
                set.InternalSet.ResetQuery();
            }
        }

        /// <summary>
        ///     Forces all DbSets to be initialized, which in turn causes o-space loading to happen
        ///     for any entity type for which we have a DbSet. This includes all DbSets that were
        ///     discovered on the user's DbContext type.
        /// </summary>
        public void ForceOSpaceLoadingForKnownEntityTypes()
        {
            if (!_oSpaceLoadingForced)
            {
                // Attempting to get o-space data for types that are not mapped is expensive so
                // only try to do it once.
                _oSpaceLoadingForced = true;

                Initialize();
                foreach (var set in _genericSets.Values.Union(_nonGenericSets.Values))
                {
                    set.InternalSet.TryInitialize();
                }
            }
        }

        /// <summary>
        ///     Performs o-space loading for the type and returns false if the type is not in the model.
        /// </summary>
        private bool TryUpdateEntitySetMappingsForType(Type entityType)
        {
            Contract.Assert(
                entityType == ObjectContextTypeCache.GetObjectType(entityType),
                "Proxy type should have been converted to real type");

            if (_entitySetMappings.ContainsKey(entityType))
            {
                return true;
            }
            // We didn't find the type on first look, but this could be because the o-space loading
            // has not happened.  So we try that, update our cached mappings, and try again.
            var typeToLoad = entityType;
            do
            {
                ObjectContext.MetadataWorkspace.LoadFromAssembly(typeToLoad.Assembly);
                typeToLoad = typeToLoad.BaseType;
            }
            while (typeToLoad != null
                   && typeToLoad != typeof(Object));

            UpdateEntitySetMappings();

            return _entitySetMappings.ContainsKey(entityType);
        }

        /// <summary>
        ///     Performs o-space loading for the type and throws if the type is not in the model.
        /// </summary>
        /// <param name="entityType"> Type of the entity. </param>
        private void UpdateEntitySetMappingsForType(Type entityType)
        {
            Contract.Assert(
                entityType == ObjectContextTypeCache.GetObjectType(entityType),
                "Proxy type should have been converted to real type");

            if (!TryUpdateEntitySetMappingsForType(entityType))
            {
                if (IsComplexType(entityType))
                {
                    throw Error.DbSet_DbSetUsedWithComplexType(entityType.Name);
                }
                if (IsPocoTypeInNonPocoAssembly(entityType))
                {
                    throw Error.DbSet_PocoAndNonPocoMixedInSameAssembly(entityType.Name);
                }
                throw Error.DbSet_EntityTypeNotInModel(entityType.Name);
            }
        }

        /// <summary>
        ///     Returns true if the given entity type does not have EdmEntityTypeAttribute but is in
        ///     an assembly that has EdmSchemaAttribute.  This indicates mixing of POCO and EOCO in the
        ///     same assembly, which is something that we don't support.
        /// </summary>
        private static bool IsPocoTypeInNonPocoAssembly(Type entityType)
        {
            return entityType.Assembly.GetCustomAttributes(typeof(EdmSchemaAttribute), inherit: false).Any() &&
                   !entityType.GetCustomAttributes(typeof(EdmEntityTypeAttribute), inherit: true).Any();
        }

        /// <summary>
        ///     Determines whether or not the given clrType is mapped to a complex type.  Assumes o-space loading has happened.
        /// </summary>
        private bool IsComplexType(Type clrType)
        {
            var metadataWorkspace = GetObjectContextWithoutDatabaseInitialization().MetadataWorkspace;
            var objectItemCollection = (ObjectItemCollection)metadataWorkspace.GetItemCollection(DataSpace.OSpace);
            var ospaceTypes = metadataWorkspace.GetItems<ComplexType>(DataSpace.OSpace);
            return ospaceTypes.Where(t => objectItemCollection.GetClrType(t) == clrType).Any();
        }

        /// <summary>
        ///     Updates the cache of types to entity sets either for the first time or after potentially
        ///     doing some o-space loading.
        /// </summary>
        private void UpdateEntitySetMappings()
        {
            var metadataWorkspace = GetObjectContextWithoutDatabaseInitialization().MetadataWorkspace;
            var objectItemCollection = (ObjectItemCollection)metadataWorkspace.GetItemCollection(DataSpace.OSpace);
            var ospaceTypes = metadataWorkspace.GetItems<EntityType>(DataSpace.OSpace);
            var inverseHierarchy = new Stack<EntityType>();

            foreach (var ospaceType in ospaceTypes)
            {
                inverseHierarchy.Clear();
                var cspaceType = (EntityType)metadataWorkspace.GetEdmSpaceType(ospaceType);
                do
                {
                    inverseHierarchy.Push(cspaceType);
                    cspaceType = (EntityType)cspaceType.BaseType;
                }
                while (cspaceType != null);

                EntitySet entitySet = null;
                while (entitySet == null
                       && inverseHierarchy.Count > 0)
                {
                    cspaceType = inverseHierarchy.Pop();
                    foreach (var container in metadataWorkspace.GetItems<EntityContainer>(DataSpace.CSpace))
                    {
                        var entitySets = container.BaseEntitySets.Where(s => s.ElementType == cspaceType);
                        var entitySetsCount = entitySets.Count();
                        if (entitySetsCount > 1
                            || entitySetsCount == 1 && entitySet != null)
                        {
                            throw Error.DbContext_MESTNotSupported();
                        }
                        if (entitySetsCount == 1)
                        {
                            entitySet = (EntitySet)entitySets.First();
                        }
                    }
                }

                // Entity set may be null if the o-space type is a base type that is in the model but is
                // not part of any set.  For most practical purposes, this type is not in the model since
                // there is no way to query etc. for objects of this type.
                if (entitySet != null)
                {
                    var ospaceBaseType = (EntityType)metadataWorkspace.GetObjectSpaceType(cspaceType);
                    var clrType = objectItemCollection.GetClrType(ospaceType);
                    var clrBaseType = objectItemCollection.GetClrType(ospaceBaseType);
                    _entitySetMappings[clrType] = new EntitySetTypePair(entitySet, clrBaseType);
                }
            }
        }

        #endregion

        #region Validation

        /// <summary>
        ///     Gets <see cref="ValidationProvider" /> instance used to create validators and validation contexts.
        ///     This property is virtual to allow mocking.
        /// </summary>
        public virtual ValidationProvider ValidationProvider
        {
            get { return _validationProvider; }
        }

        #endregion

        public virtual string DefaultSchema
        {
            get { return null; }
        }
    }

    [ContractClassFor(typeof(InternalContext))]
    internal abstract class InternalContextContracts : InternalContext
    {
        protected InternalContextContracts()
            : base(null)
        {
        }

        public override ObjectContext ObjectContext
        {
            get { throw new NotImplementedException(); }
        }

        public override ObjectContext GetObjectContextWithoutDatabaseInitialization()
        {
            throw new NotImplementedException();
        }

        public override ClonedObjectContext CreateObjectContextForDdlOps()
        {
            throw new NotImplementedException();
        }

        protected override void InitializeContext()
        {
            throw new NotImplementedException();
        }

        protected override void InitializeDatabase()
        {
            throw new NotImplementedException();
        }

        public override void MarkDatabaseInitialized()
        {
            throw new NotImplementedException();
        }

        public override IDatabaseInitializer<DbContext> DefaultInitializer
        {
            get { throw new NotImplementedException(); }
        }

        public override bool LazyLoadingEnabled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override bool ProxyCreationEnabled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override DbConnection Connection
        {
            get { throw new NotImplementedException(); }
        }

        public override DbConnectionStringOrigin ConnectionStringOrigin
        {
            get { throw new NotImplementedException(); }
        }

        public override void OverrideConnection(IInternalConnection connection)
        {
            Contract.Requires(connection != null);
            throw new NotImplementedException();
        }
    }
}
