﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    ///     Defines behavior for implementations of IQueryable that allow modifications to the membership of the resulting set.
    /// </summary>
    /// <typeparam name="TEntity"> Type of entities returned from the queryable. </typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public interface IObjectSet<TEntity> : IQueryable<TEntity>
        where TEntity : class
    {
        /// <summary>
        ///     Notify the set that an object representing a new entity should be added to the set.
        ///     Depending on the implementation, the change to the set may not be visible in an enumeration of the set 
        ///     until changes to that set have been persisted in some manner.
        /// </summary>
        /// <param name="entity"> Entity object to be added. </param>
        void AddObject(TEntity entity);

        /// <summary>
        ///     Notify the set that an object representing a new entity should be added to the set.
        ///     Depending on the implementation, the change to the set may not be visible in an enumeration of the set 
        ///     until changes to that set have been persisted in some manner.
        /// </summary>
        /// <param name="entity"> Entity object to be attached. </param>
        void Attach(TEntity entity);

        /// <summary>
        ///     Notify the set that an object representing an entity should be removed from the set.
        ///     Depending on the implementation, the change to the set may not be visible in an enumeration of the set 
        ///     until changes to that set have been persisted in some manner.
        /// </summary>
        /// <param name="entity"> Entity object to be removed. </param>
        /// <exception cref="InvalidOperationException">Throws if the specified object is not in the set.</exception>
        void DeleteObject(TEntity entity);

        /// <summary>
        ///     Notify the set that an object representing an entity should be removed from the set.
        ///     Depending on the implementation, the change to the set may not be visible in an enumeration of the set 
        ///     until changes to that set have been persisted in some manner.
        /// </summary>
        /// <param name="entity"> Entity object to be detached. </param>
        void Detach(TEntity entity);
    }
}
