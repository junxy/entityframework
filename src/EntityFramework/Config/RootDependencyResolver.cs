// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Config
{
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Internal;
    using System.Diagnostics.Contracts;

    /// <summary>
    ///     This resolver is always the last resolver in the internal resolver chain and is
    ///     responsible for providing the default service for each dependency or throwing an
    ///     exception if there is no reasonable default service.
    /// </summary>
    internal class RootDependencyResolver : IDbDependencyResolver
    {
        private readonly ResolverChain _resolvers = new ResolverChain();
        private readonly DatabaseInitializerResolver _databaseInitializerResolver;

        public RootDependencyResolver()
            : this(new MigrationsConfigurationResolver(), new DefaultProviderServicesResolver(), new DatabaseInitializerResolver())
        {
        }

        public RootDependencyResolver(
            MigrationsConfigurationResolver migrationsConfigurationResolver,
            DefaultProviderServicesResolver defaultProviderServicesResolver,
            DatabaseInitializerResolver databaseInitializerResolver)
        {
            Contract.Requires(migrationsConfigurationResolver != null);
            Contract.Requires(defaultProviderServicesResolver != null);
            Contract.Requires(databaseInitializerResolver != null);

            _databaseInitializerResolver = databaseInitializerResolver;

            _resolvers.Add(_databaseInitializerResolver);
            _resolvers.Add(migrationsConfigurationResolver);
            _resolvers.Add(new CachingDependencyResolver(defaultProviderServicesResolver));
            _resolvers.Add(new SingletonDependencyResolver<IDbConnectionFactory>(new SqlConnectionFactory()));
            _resolvers.Add(new SingletonDependencyResolver<IDbModelCacheKeyFactory>(new DefaultModelCacheKeyFactory()));
        }

        public DatabaseInitializerResolver DatabaseInitializerResolver
        {
            get { return _databaseInitializerResolver; }
        }

        /// <inheritdoc />
        public virtual object GetService(Type type, string name)
        {
            return _resolvers.GetService(type, name);
        }

        /// <inheritdoc />
        public virtual void Release(object service)
        {
            _resolvers.Release(service);
        }
    }
}
