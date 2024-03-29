// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm.UnitTests
{
    using System.Data.Entity.Edm;
    using Xunit;

    public sealed class EdmEntitySetExtensionsTests
    {
        [Fact]
        public void Can_get_and_set_configuration_annotation()
        {
            var entitySet = new EdmEntitySet();

            entitySet.SetConfiguration(42);

            Assert.Equal(42, entitySet.GetConfiguration());
        }
    }
}
