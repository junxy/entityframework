﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.Validation
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.Contracts;

    /// <summary>
    ///     Contains information needed to validate an entity or its properties.
    /// </summary>
    internal class EntityValidationContext
    {
        /// <summary>
        ///     The entity being validated or the entity that owns the property being validated.
        /// </summary>
        private readonly InternalEntityEntry _entityEntry;

        /// <summary>
        ///     Initializes a new instance of EntityValidationContext class.
        /// </summary>
        /// <param name="entityEntry"> The entity being validated or the entity that owns the property being validated. </param>
        /// <param name="externalValidationContexts"> External contexts needed for validation. </param>
        public EntityValidationContext(InternalEntityEntry entityEntry, ValidationContext externalValidationContext)
        {
            Contract.Requires(entityEntry != null);
            Contract.Requires(externalValidationContext != null);

            _entityEntry = entityEntry;
            ExternalValidationContext = externalValidationContext;
        }

        /// <summary>
        ///     External context needed for validation.
        /// </summary>
        public ValidationContext ExternalValidationContext { get; private set; }

        /// <summary>
        ///     Gets the entity being validated or the entity that owns the property being validated.
        /// </summary>
        public InternalEntityEntry InternalEntity
        {
            get { return _entityEntry; }
        }
    }
}
