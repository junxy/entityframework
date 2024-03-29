// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Common.Utils;
    using System.Diagnostics.Contracts;

    /// <summary>
    ///     The Row class is intended to provide a constructor-like means of calling <see cref="DbExpressionBuilder.NewRow" />.
    /// </summary>
    public sealed class Row
    {
        private readonly ReadOnlyCollection<KeyValuePair<string, DbExpression>> arguments;

        /// <summary>
        ///     Constructs a new Row with the specified first column value and optional successive column values
        /// </summary>
        /// <param name="columnValue"> A key-value pair that provides the first column in the new row instance (required) </param>
        /// <param name="columnValues"> Key-value pairs that provide any subsequent columns in the new row instance (optional) </param>
        public Row(KeyValuePair<string, DbExpression> columnValue, params KeyValuePair<string, DbExpression>[] columnValues)
        {
            arguments = new ReadOnlyCollection<KeyValuePair<string, DbExpression>>(Helpers.Prepend(columnValues, columnValue));
        }

        /// <summary>
        ///     Creates a new <see cref="DbNewInstanceExpression" /> that constructs a new row based on the columns
        ///     contained in this Row instance.
        /// </summary>
        /// <returns> A new DbNewInstanceExpression that constructs a row with the same column names and DbExpression values as this Row instance </returns>
        /// <seealso cref="DbExpressionBuilder.NewRow" />
        public DbNewInstanceExpression ToExpression()
        {
            return DbExpressionBuilder.NewRow(arguments);
        }

        /// <summary>
        ///     Converts the given Row instance into an instance of <see cref="DbExpression" />
        /// </summary>
        /// <param name="row"> </param>
        /// <returns> A DbExpression based on the Row instance </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="row" />
        ///     is null.</exception>
        /// <seealso cref="ToExpression" />
        public static implicit operator DbExpression(Row row)
        {
            Contract.Requires(row != null);
            return row.ToExpression();
        }
    }
}
