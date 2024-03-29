// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Config
{
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Migrations.Extensions;
    using System.Data.Entity.Resources;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    ///     A class derived from this class can be placed in the same assembly as a class derived from
    ///     <see cref="DbContext" /> to define Entity Framework configuration for an application.
    ///     Configuration is set by calling protected methods and setting protected properties of this
    ///     class in the constructor of your derived type.
    ///     The type to use can also be registered in the config file of the application.
    ///     See http://go.microsoft.com/fwlink/?LinkId=260883 for more information about Entity Framework configuration.
    /// </summary>
    [SuppressMessage("Microsoft.Contracts", "CC1036",
        Justification = "Due to a bug in code contracts IsNullOrWhiteSpace isn't recognized as pure.")]
    public class DbConfiguration
    {
        private CompositeResolver<ResolverChain, ResolverChain> _resolvers;
        private RootDependencyResolver _rootResolver;

        // This does not need to be volatile since it only protects against inappropriate use not
        // thread-unsafe use.
        private bool _isLocked;

        /// <summary>
        ///     Any class derived from <see cref="DbConfiguration" /> must have a public parameterless constructor
        ///     and that constructor should call this constructor.
        /// </summary>
        protected internal DbConfiguration()
            : this(new ResolverChain(), new ResolverChain(), new RootDependencyResolver())
        {
            _resolvers.First.Add(new AppConfigDependencyResolver(AppConfig.DefaultInstance));
        }

        internal DbConfiguration(ResolverChain appConfigChain, ResolverChain normalResolverChain, RootDependencyResolver rootResolver)
        {
            Contract.Requires(appConfigChain != null);
            Contract.Requires(normalResolverChain != null);

            _rootResolver = rootResolver;
            _resolvers = new CompositeResolver<ResolverChain, ResolverChain>(appConfigChain, normalResolverChain);
            _resolvers.Second.Add(_rootResolver);
        }

        /// <summary>
        ///     The Singleton instance of <see cref="DbConfiguration" /> for this app domain. This can be
        ///     set at application start before any Entity Framework features have been used and afterwards
        ///     should be treated as read-only.
        /// </summary>
        public static DbConfiguration Instance
        {
            // Note that GetConfiguration and SetConfiguration on DbConfigurationManager are thread-safe.
            get { return DbConfigurationManager.Instance.GetConfiguration(); }
            set
            {
                Contract.Requires(value != null);

                DbConfigurationManager.Instance.SetConfiguration(value);
            }
        }

        internal virtual void Lock()
        {
            _isLocked = true;
        }

        internal virtual void AddAppConfigResolver(IDbDependencyResolver resolver)
        {
            Contract.Requires(resolver != null);

            _resolvers.First.Add(resolver);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to
        ///     add a <see cref="IDbDependencyResolver" /> instance to the Chain of Responsibility of resolvers that
        ///     are used to resolve dependencies needed by the Entity Framework.
        /// </summary>
        /// <remarks>
        ///     Resolvers are asked to resolve dependencies in reverse order from which they are added. This means
        ///     that a resolver can be added to override resolution of a dependency that would already have been
        ///     resolved in a different way.
        ///     The only exception to this is that any dependency registered in the application's config file
        ///     will always be used in preference to using a dependency resolver added here.
        /// </remarks>
        /// <param name="resolver"> The resolver to add. </param>
        protected internal void AddDependencyResolver(IDbDependencyResolver resolver)
        {
            Contract.Requires(resolver != null);
            CheckNotLocked("AddDependencyResolver");

            // New resolvers always run after the config resolvers so that config always wins over code
            _resolvers.Second.Add(resolver);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to register
        ///     an Entity Framework provider.
        /// </summary>
        /// <param name="providerInvariantName"> The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this provider will be used. </param>
        /// <param name="provider"> The provider instance. </param>
        [CLSCompliant(false)]
        protected internal void AddProvider(string providerInvariantName, DbProviderServices provider)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(providerInvariantName));
            Contract.Requires(provider != null);
            CheckNotLocked("AddProvider");

            AddDependencyResolver(new SingletonDependencyResolver<DbProviderServices>(provider, providerInvariantName));
        }

        /// <summary>
        ///     Gets the Entity Framework provider that has been registered for use with ADO.NET connections that are
        ///     identified by the given ADO.NET provider invariant name.
        /// </summary>
        /// <param name="providerInvariantName"> The provider invariant name. </param>
        /// <returns> The registered provider. </returns>
        [CLSCompliant(false)]
        public DbProviderServices GetProvider(string providerInvariantName)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(providerInvariantName));

            return _resolvers.GetService<DbProviderServices>(providerInvariantName);
        }

        /// <summary>
        ///     The <see cref="IDbConnectionFactory" /> that is used to create connections by convention if no other
        ///     connection string or connection is given to or can be discovered by <see cref="DbContext" />.
        ///     Set this property from the constructor of a class derived from <see cref="DbConfiguration" /> to change
        ///     the default connection factory being used.
        /// </summary>
        public IDbConnectionFactory DefaultConnectionFactory
        {
            protected internal set
            {
                Contract.Requires(value != null);
                CheckNotLocked("DefaultConnectionFactory");

                AddDependencyResolver(new SingletonDependencyResolver<IDbConnectionFactory>(value));
            }
            get
            {
                return Database.DefaultConnectionFactoryChanged
#pragma warning disable 612,618
                           ? Database.DefaultConnectionFactory
#pragma warning restore 612,618
                           : _resolvers.GetService<IDbConnectionFactory>();
            }
        }

        public IDbModelCacheKeyFactory ModelCacheKeyFactory
        {
            protected internal set
            {
                Contract.Requires(value != null);
                CheckNotLocked("ModelCacheKeyFactory");

                AddDependencyResolver(new SingletonDependencyResolver<IDbModelCacheKeyFactory>(value));
            }
            get { return _resolvers.GetService<IDbModelCacheKeyFactory>(); }
        }

        /// <summary>
        ///     Gets the <see cref="IDbDependencyResolver" /> that is being used to resolve service
        ///     dependencies in the Entity Framework.
        /// </summary>
        public virtual IDbDependencyResolver DependencyResolver
        {
            get { return _resolvers; }
        }

        internal virtual RootDependencyResolver RootResolver
        {
            get { return _rootResolver; }
        }

        /// <summary>
        ///     This method is not thread-safe and should only be used to switch in a different root resolver
        ///     before the configuration is locked and set. It is used for pushing a new configuration by
        ///     DbContextInfo while maintaining legacy settings (such as database initializers) that are
        ///     set on the root resolver.
        /// </summary>
        internal virtual void SwitchInRootResolver(RootDependencyResolver value)
        {
            Contract.Requires(value != null);

            Contract.Assert(!_isLocked);

            // The following is not thread-safe but this code is only called when pushing a configuration
            // and happens to a new DbConfiguration before it has been set and locked.
            var newChain = new ResolverChain();
            newChain.Add(value);
            _resolvers.Second.Resolvers.Skip(1).Each(newChain.Add);

            _rootResolver = value;
            _resolvers = new CompositeResolver<ResolverChain, ResolverChain>(_resolvers.First, newChain);
        }

        private void CheckNotLocked(string memberName)
        {
            if (_isLocked)
            {
                throw new InvalidOperationException(Strings.ConfigurationLocked(memberName));
            }
        }
    }
}
