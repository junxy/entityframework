﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Data.Entity.ModelConfiguration.Internal.UnitTests;
    using Moq;
    using ProductivityApiUnitTests;

    /// <summary>
    ///     A derived InternalReferenceEntry implementation that exposes a parameterless constructor for mocking.
    /// </summary>
    internal abstract class InternalReferenceEntryForMock : InternalReferenceEntry
    {
        private static readonly NavigationEntryMetadata _fakeReferenceMetadata =
            new NavigationEntryMetadata(typeof(PropertyApiTests.FakeWithProps), typeof(FakeEntity), "Reference", isCollection: false);

        protected InternalReferenceEntryForMock()
            : base(new Mock<PropertyApiTests.InternalEntityEntryForMock<FakeEntity>>().Object, _fakeReferenceMetadata)
        {
        }
    }
}
