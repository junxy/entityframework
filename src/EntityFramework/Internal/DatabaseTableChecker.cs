﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Migrations.History;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Transactions;

    internal class DatabaseTableChecker
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool AnyModelTableExists(DbContext context)
        {
            try
            {
                var internalContext = context.InternalContext;

                if (internalContext.CodeFirstModel == null)
                {
                    return true;
                }

                var providerName = internalContext.ProviderName;
                IPseudoProvider provider;

                switch (providerName)
                {
                    case "System.Data.SqlClient":
                        provider = new SqlPseudoProvider();
                        break;

                    case "System.Data.SqlServerCe.4.0":
                        provider = new SqlCePseudoProvider();
                        break;

                    default:
                        return true;
                }

                var modelTables = GetModelTables(internalContext.ObjectContext.MetadataWorkspace).ToList();

                if (!modelTables.Any())
                {
                    return true;
                }

                IEnumerable<Tuple<string, string>> databaseTables;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    using (var clonedObjectContext = internalContext.CreateObjectContextForDdlOps())
                    {
                        databaseTables = GetDatabaseTables(clonedObjectContext.Connection, provider).ToList();
                    }
                }

                if (databaseTables.Any(
                    t => t.Item2 == HistoryContext.TableName
                         || t.Item2 == EdmMetadataContext.TableName))
                {
                    return true;
                }

                var comparer = provider.SupportsSchemas
                                   ? EqualityComparer<Tuple<string, string>>.Default
                                   : (IEqualityComparer<Tuple<string, string>>)new IgnoreSchemaComparer();

                foreach (var databaseTable in databaseTables)
                {
                    if (modelTables.Contains(databaseTable, comparer))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message, ex.ToString());

                // Revert to previous behavior on error
                return true;
            }
        }

        private static IEnumerable<Tuple<string, string>> GetModelTables(MetadataWorkspace workspace)
        {
            var tables = workspace
                .GetItemCollection(DataSpace.SSpace)
                .GetItems<EntityContainer>()
                .Single()
                .BaseEntitySets
                .OfType<EntitySet>()
                .Where(
                    s => !s.MetadataProperties.Contains("Type")
                         || (string)s.MetadataProperties["Type"].Value == "Tables");

            foreach (var table in tables)
            {
                var schemaName = (string)table.MetadataProperties["Schema"].Value;
                var tableName = table.MetadataProperties.Contains("Table")
                                && table.MetadataProperties["Table"].Value != null
                                    ? (string)table.MetadataProperties["Table"].Value
                                    : table.Name;

                yield return Tuple.Create(schemaName, tableName);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static IEnumerable<Tuple<string, string>> GetDatabaseTables(DbConnection connection, IPseudoProvider provider)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = provider.StoreSchemaTablesQuery;
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return Tuple.Create(
                            reader["SchemaName"] as string,
                            reader["Name"] as string);
                    }
                }
            }
        }

        private interface IPseudoProvider
        {
            string StoreSchemaTablesQuery { get; }
            bool SupportsSchemas { get; }
        }

        private class SqlPseudoProvider : IPseudoProvider
        {
            public string StoreSchemaTablesQuery
            {
                get { return "SELECT TABLE_SCHEMA SchemaName, TABLE_NAME Name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"; }
            }

            public bool SupportsSchemas
            {
                get { return true; }
            }
        }

        private class SqlCePseudoProvider : IPseudoProvider
        {
            public string StoreSchemaTablesQuery
            {
                get { return "SELECT TABLE_SCHEMA SchemaName, TABLE_NAME Name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'TABLE'"; }
            }

            public bool SupportsSchemas
            {
                get { return false; }
            }
        }

        private class IgnoreSchemaComparer : IEqualityComparer<Tuple<string, string>>
        {
            public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
            {
                return EqualityComparer<string>.Default.Equals(x.Item2, y.Item2);
            }

            public int GetHashCode(Tuple<string, string> obj)
            {
                return EqualityComparer<string>.Default.GetHashCode(obj.Item2);
            }
        }
    }
}
