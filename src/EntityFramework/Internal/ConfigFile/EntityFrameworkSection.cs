﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.ConfigFile
{
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Represents all Entity Framework related configuration
    /// </summary>
    internal class EntityFrameworkSection : ConfigurationSection
    {
        private const string DefaultConnectionFactoryKey = "defaultConnectionFactory";
        private const string ContextsKey = "contexts";
        private const string ProviderKey = "providers";
        private const string DbConfigurationKey = "dbConfiguration";

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [ConfigurationProperty(DefaultConnectionFactoryKey)]
        public virtual DefaultConnectionFactoryElement DefaultConnectionFactory
        {
            get { return (DefaultConnectionFactoryElement)this[DefaultConnectionFactoryKey]; }
            set { this[DefaultConnectionFactoryKey] = value; }
        }

        [ConfigurationProperty(DbConfigurationKey)]
        public virtual DbConfigurationElement DbConfiguration
        {
            get { return (DbConfigurationElement)this[DbConfigurationKey]; }
            set { this[DbConfigurationKey] = value; }
        }

        [ConfigurationProperty(ProviderKey)]
        public virtual ProviderCollection Providers
        {
            get { return (ProviderCollection)base[ProviderKey]; }
        }

        [ConfigurationProperty(ContextsKey)]
        public virtual ContextCollection Contexts
        {
            get { return (ContextCollection)base[ContextsKey]; }
        }
    }
}
