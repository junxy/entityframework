﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using Moq;

    /// <summary>
    ///     A derived InternalContext implementation that exposes a parameterless constructor
    ///     that creates a mocked underlying DbContext such that the internal context can
    ///     also be mocked.
    /// </summary>
    internal abstract class InternalContextForMock<TContext> : InternalContext
        where TContext : DbContext
    {
        protected InternalContextForMock()
            : base(new Mock<TContext>().Object)
        {
            Mock.Get((TContext)Owner).Setup(c => c.InternalContext).Returns(this);
        }
    }
}
