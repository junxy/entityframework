// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.CommandTrees
{
    using System.Diagnostics.Contracts;

    /// <summary>
    ///     The expression visitor pattern abstract base class that should be implemented by visitors that do not return a result value.
    /// </summary>
    [ContractClass(typeof(DbExpressionVisitorContracts))]
    public abstract class DbExpressionVisitor
    {
        /// <summary>
        ///     Called when an expression of an otherwise unrecognized type is encountered.
        /// </summary>
        /// <param name="expression"> The expression </param>
        public abstract void Visit(DbExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbAndExpression.
        /// </summary>
        /// <param name="expression"> The DbAndExpression that is being visited. </param>
        public abstract void Visit(DbAndExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbApplyExpression.
        /// </summary>
        /// <param name="expression"> The DbApplyExpression that is being visited. </param>
        public abstract void Visit(DbApplyExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbArithmeticExpression.
        /// </summary>
        /// <param name="expression"> The DbArithmeticExpression that is being visited. </param>
        public abstract void Visit(DbArithmeticExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbCaseExpression.
        /// </summary>
        /// <param name="expression"> The DbCaseExpression that is being visited. </param>
        public abstract void Visit(DbCaseExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbCastExpression.
        /// </summary>
        /// <param name="expression"> The DbCastExpression that is being visited. </param>
        public abstract void Visit(DbCastExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbComparisonExpression.
        /// </summary>
        /// <param name="expression"> The DbComparisonExpression that is being visited. </param>
        public abstract void Visit(DbComparisonExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbConstantExpression.
        /// </summary>
        /// <param name="expression"> The DbConstantExpression that is being visited. </param>
        public abstract void Visit(DbConstantExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbCrossJoinExpression.
        /// </summary>
        /// <param name="expression"> The DbCrossJoinExpression that is being visited. </param>
        public abstract void Visit(DbCrossJoinExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbDerefExpression.
        /// </summary>
        /// <param name="expression"> The DbDerefExpression that is being visited. </param>
        public abstract void Visit(DbDerefExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbDistinctExpression.
        /// </summary>
        /// <param name="expression"> The DbDistinctExpression that is being visited. </param>
        public abstract void Visit(DbDistinctExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbElementExpression.
        /// </summary>
        /// <param name="expression"> The DbElementExpression that is being visited. </param>
        public abstract void Visit(DbElementExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbExceptExpression.
        /// </summary>
        /// <param name="expression"> The DbExceptExpression that is being visited. </param>
        public abstract void Visit(DbExceptExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbFilterExpression.
        /// </summary>
        /// <param name="expression"> The DbFilterExpression that is being visited. </param>
        public abstract void Visit(DbFilterExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbFunctionExpression
        /// </summary>
        /// <param name="expression"> The DbFunctionExpression that is being visited. </param>
        public abstract void Visit(DbFunctionExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbEntityRefExpression.
        /// </summary>
        /// <param name="expression"> The DbEntityRefExpression that is being visited. </param>
        public abstract void Visit(DbEntityRefExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbRefKeyExpression.
        /// </summary>
        /// <param name="expression"> The DbRefKeyExpression that is being visited. </param>
        public abstract void Visit(DbRefKeyExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbGroupByExpression.
        /// </summary>
        /// <param name="expression"> The DbGroupByExpression that is being visited. </param>
        public abstract void Visit(DbGroupByExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbIntersectExpression.
        /// </summary>
        /// <param name="expression"> The DbIntersectExpression that is being visited. </param>
        public abstract void Visit(DbIntersectExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbIsEmptyExpression.
        /// </summary>
        /// <param name="expression"> The DbIsEmptyExpression that is being visited. </param>
        public abstract void Visit(DbIsEmptyExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbIsNullExpression.
        /// </summary>
        /// <param name="expression"> The DbIsNullExpression that is being visited. </param>
        public abstract void Visit(DbIsNullExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbIsOfExpression.
        /// </summary>
        /// <param name="expression"> The DbIsOfExpression that is being visited. </param>
        public abstract void Visit(DbIsOfExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbJoinExpression.
        /// </summary>
        /// <param name="expression"> The DbJoinExpression that is being visited. </param>
        public abstract void Visit(DbJoinExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbLambdaExpression.
        /// </summary>
        /// <param name="expression"> The DbLambdaExpression that is being visited. </param>
        public virtual void Visit(DbLambdaExpression expression)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Visitor pattern method for DbLikeExpression.
        /// </summary>
        /// <param name="expression"> The DbLikeExpression that is being visited. </param>
        public abstract void Visit(DbLikeExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbLimitExpression.
        /// </summary>
        /// <param name="expression"> The DbLimitExpression that is being visited. </param>
        public abstract void Visit(DbLimitExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbNewInstanceExpression.
        /// </summary>
        /// <param name="expression"> The DbNewInstanceExpression that is being visited. </param>
        public abstract void Visit(DbNewInstanceExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbNotExpression.
        /// </summary>
        /// <param name="expression"> The DbNotExpression that is being visited. </param>
        public abstract void Visit(DbNotExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbNullExpression.
        /// </summary>
        /// <param name="expression"> The DbNullExpression that is being visited. </param>
        public abstract void Visit(DbNullExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbOfTypeExpression.
        /// </summary>
        /// <param name="expression"> The DbOfTypeExpression that is being visited. </param>
        public abstract void Visit(DbOfTypeExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbOrExpression.
        /// </summary>
        /// <param name="expression"> The DbOrExpression that is being visited. </param>
        public abstract void Visit(DbOrExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbParameterReferenceExpression.
        /// </summary>
        /// <param name="expression"> The DbParameterReferenceExpression that is being visited. </param>
        public abstract void Visit(DbParameterReferenceExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbProjectExpression.
        /// </summary>
        /// <param name="expression"> The DbProjectExpression that is being visited. </param>
        public abstract void Visit(DbProjectExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbPropertyExpression.
        /// </summary>
        /// <param name="expression"> The DbPropertyExpression that is being visited. </param>
        public abstract void Visit(DbPropertyExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbQuantifierExpression.
        /// </summary>
        /// <param name="expression"> The DbQuantifierExpression that is being visited. </param>
        public abstract void Visit(DbQuantifierExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbRefExpression.
        /// </summary>
        /// <param name="expression"> The DbRefExpression that is being visited. </param>
        public abstract void Visit(DbRefExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbRelationshipNavigationExpression.
        /// </summary>
        /// <param name="expression"> The DbRelationshipNavigationExpression that is being visited. </param>
        public abstract void Visit(DbRelationshipNavigationExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbScanExpression.
        /// </summary>
        /// <param name="expression"> The DbScanExpression that is being visited. </param>
        public abstract void Visit(DbScanExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbSkipExpression.
        /// </summary>
        /// <param name="expression"> The DbSkipExpression that is being visited. </param>
        public abstract void Visit(DbSkipExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbSortExpression.
        /// </summary>
        /// <param name="expression"> The DbSortExpression that is being visited. </param>
        public abstract void Visit(DbSortExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbTreatExpression.
        /// </summary>
        /// <param name="expression"> The DbTreatExpression that is being visited. </param>
        public abstract void Visit(DbTreatExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbUnionAllExpression.
        /// </summary>
        /// <param name="expression"> The DbUnionAllExpression that is being visited. </param>
        public abstract void Visit(DbUnionAllExpression expression);

        /// <summary>
        ///     Visitor pattern method for DbVariableReferenceExpression.
        /// </summary>
        /// <param name="expression"> The DbVariableReferenceExpression that is being visited. </param>
        public abstract void Visit(DbVariableReferenceExpression expression);
    }

    [ContractClassFor(typeof(DbExpressionVisitor))]
    internal abstract class DbExpressionVisitorContracts : DbExpressionVisitor
    {
        public override void Visit(DbExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbAndExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbApplyExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbArithmeticExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbCaseExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbCastExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbComparisonExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbConstantExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbCrossJoinExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbDerefExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbDistinctExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbElementExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbExceptExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbFilterExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbFunctionExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbEntityRefExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbRefKeyExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbGroupByExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbIntersectExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbIsEmptyExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbIsNullExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbIsOfExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbJoinExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbLikeExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbLimitExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbNewInstanceExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbNotExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbNullExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbOfTypeExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbOrExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbParameterReferenceExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbProjectExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbPropertyExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbQuantifierExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbRefExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbRelationshipNavigationExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbScanExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbSkipExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbSortExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbTreatExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbUnionAllExpression expression)
        {
            Contract.Requires(expression != null);
        }

        public override void Visit(DbVariableReferenceExpression expression)
        {
            Contract.Requires(expression != null);
        }
    }
}
