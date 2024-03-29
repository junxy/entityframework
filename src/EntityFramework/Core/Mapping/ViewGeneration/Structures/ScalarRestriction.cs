// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.ViewGeneration.Structures
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Resources;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using DomainBoolExpr =
        System.Data.Entity.Core.Common.Utils.Boolean.BoolExpr<Common.Utils.Boolean.DomainConstraint<BoolLiteral, Constant>>;

    /// <summary>
    ///     A class that denotes the boolean expression: "scalarVar in values".
    ///     See the comments in <see cref="MemberRestriction" /> for complete and incomplete restriction objects.
    /// </summary>
    internal class ScalarRestriction : MemberRestriction
    {
        #region Constructors

        /// <summary>
        ///     Creates a scalar member restriction with the meaning "<paramref name="member" /> = <paramref name="value" />".
        ///     This constructor is used for creating discriminator type conditions.
        /// </summary>
        internal ScalarRestriction(MemberPath member, Constant value)
            : base(new MemberProjectedSlot(member), value)
        {
            Debug.Assert(
                value is ScalarConstant || value.IsNull() || value.IsNotNull(), "value is expected to be ScalarConstant, NULL, or NOT_NULL.");
        }

        /// <summary>
        ///     Creates a scalar member restriction with the meaning "<paramref name="member" /> in <paramref name="values" />".
        /// </summary>
        internal ScalarRestriction(MemberPath member, IEnumerable<Constant> values, IEnumerable<Constant> possibleValues)
            : base(new MemberProjectedSlot(member), values, possibleValues)
        {
        }

        /// <summary>
        ///     Creates a scalar member restriction with the meaning "<paramref name="slot" /> in <paramref name="domain" />".
        /// </summary>
        internal ScalarRestriction(MemberProjectedSlot slot, Domain domain)
            : base(slot, domain)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Fixes the range of the restriction in accordance with <paramref name="range" />.
        ///     Member restriction must be complete for this operation.
        /// </summary>
        internal override DomainBoolExpr FixRange(Set<Constant> range, MemberDomainMap memberDomainMap)
        {
            Debug.Assert(IsComplete, "Ranges are fixed only for complete scalar restrictions.");
            var newPossibleValues = memberDomainMap.GetDomain(RestrictedMemberSlot.MemberPath);
            BoolLiteral newLiteral = new ScalarRestriction(RestrictedMemberSlot, new Domain(range, newPossibleValues));
            return newLiteral.GetDomainBoolExpression(memberDomainMap);
        }

        internal override BoolLiteral RemapBool(Dictionary<MemberPath, MemberPath> remap)
        {
            var newVar = RestrictedMemberSlot.RemapSlot(remap);
            return new ScalarRestriction(newVar, Domain);
        }

        internal override MemberRestriction CreateCompleteMemberRestriction(IEnumerable<Constant> possibleValues)
        {
            Debug.Assert(!IsComplete, "CreateCompleteMemberRestriction must be called only for incomplete restrictions.");
            return new ScalarRestriction(RestrictedMemberSlot, new Domain(Domain.Values, possibleValues));
        }

        internal override StringBuilder AsEsql(StringBuilder builder, string blockAlias, bool skipIsNotNull)
        {
            return ToStringHelper(builder, blockAlias, skipIsNotNull, false);
        }

        internal override DbExpression AsCqt(DbExpression row, bool skipIsNotNull)
        {
            DbExpression cqt = null;

            AsCql(
                // negatedConstantAsCql action
                (negated, domainValues) =>
                    {
                        Debug.Assert(cqt == null, "unexpected construction order - cqt must be null");
                        cqt = negated.AsCqt(row, domainValues, RestrictedMemberSlot.MemberPath, skipIsNotNull);
                    },
                // varInDomain action
                (domainValues) =>
                    {
                        Debug.Assert(cqt == null, "unexpected construction order - cqt must be null");
                        Debug.Assert(domainValues.Count > 0, "domain must not be empty");
                        cqt = RestrictedMemberSlot.MemberPath.AsCqt(row);
                        if (domainValues.Count == 1)
                        {
                            // Single value
                            cqt = cqt.Equal(domainValues.Single().AsCqt(row, RestrictedMemberSlot.MemberPath));
                        }
                        else
                        {
                            // Multiple values: build list of var = c1, var = c2, ..., then OR them all.
                            var operands =
                                domainValues.Select(c => (DbExpression)cqt.Equal(c.AsCqt(row, RestrictedMemberSlot.MemberPath))).ToList();
                            cqt = Helpers.BuildBalancedTreeInPlace(operands, (prev, next) => prev.Or(next));
                        }
                    },
                // varIsNotNull action
                () =>
                    {
                        // ( ... AND var IS NOT NULL)
                        DbExpression varIsNotNull = RestrictedMemberSlot.MemberPath.AsCqt(row).IsNull().Not();
                        cqt = cqt != null ? cqt.And(varIsNotNull) : varIsNotNull;
                    },
                // varIsNull action
                () =>
                    {
                        // (var IS NULL OR ...)
                        DbExpression varIsNull = RestrictedMemberSlot.MemberPath.AsCqt(row).IsNull();
                        cqt = cqt != null ? varIsNull.Or(cqt) : varIsNull;
                    },
                skipIsNotNull);

            return cqt;
        }

        internal override StringBuilder AsUserString(StringBuilder builder, string blockAlias, bool skipIsNotNull)
        {
            return ToStringHelper(builder, blockAlias, skipIsNotNull, true);
        }

        /// <summary>
        ///     Common code for <see cref="AsEsql" /> and <see cref="AsUserString" /> methods.
        /// </summary>
        private StringBuilder ToStringHelper(StringBuilder inputBuilder, string blockAlias, bool skipIsNotNull, bool userString)
        {
            // Due to the varIsNotNull and varIsNull actions, we cannot build incrementally.
            // So we use a local StringBuilder - it should not be that inefficient (one extra copy).
            var builder = new StringBuilder();

            AsCql(
                // negatedConstantAsCql action
                (negated, domainValues) =>
                    {
                        if (userString)
                        {
                            negated.AsUserString(builder, blockAlias, domainValues, RestrictedMemberSlot.MemberPath, skipIsNotNull);
                        }
                        else
                        {
                            negated.AsEsql(builder, blockAlias, domainValues, RestrictedMemberSlot.MemberPath, skipIsNotNull);
                        }
                    },
                // varInDomain action
                (domainValues) =>
                    {
                        Debug.Assert(domainValues.Count > 0, "domain must not be empty");
                        RestrictedMemberSlot.MemberPath.AsEsql(builder, blockAlias);
                        if (domainValues.Count == 1)
                        {
                            // Single value
                            builder.Append(" = ");
                            if (userString)
                            {
                                domainValues.Single().ToCompactString(builder);
                            }
                            else
                            {
                                domainValues.Single().AsEsql(builder, RestrictedMemberSlot.MemberPath, blockAlias);
                            }
                        }
                        else
                        {
                            // Multiple values
                            builder.Append(" IN {");
                            var first = true;
                            foreach (var constant in domainValues)
                            {
                                if (!first)
                                {
                                    builder.Append(", ");
                                }
                                if (userString)
                                {
                                    constant.ToCompactString(builder);
                                }
                                else
                                {
                                    constant.AsEsql(builder, RestrictedMemberSlot.MemberPath, blockAlias);
                                }
                                first = false;
                            }
                            builder.Append('}');
                        }
                    },
                // varIsNotNull action
                () =>
                    {
                        // (leftExpr AND var IS NOT NULL)
                        var leftExprEmpty = builder.Length == 0;
                        builder.Insert(0, '(');
                        if (!leftExprEmpty)
                        {
                            builder.Append(" AND ");
                        }
                        if (userString)
                        {
                            RestrictedMemberSlot.MemberPath.ToCompactString(builder, Strings.ViewGen_EntityInstanceToken);
                            builder.Append(" is not NULL)"); // plus the closing bracket
                        }
                        else
                        {
                            RestrictedMemberSlot.MemberPath.AsEsql(builder, blockAlias);
                            builder.Append(" IS NOT NULL)"); // plus the closing bracket
                        }
                    },
                // varIsNull action
                () =>
                    {
                        // (var IS NULL OR rightExpr)
                        var rightExprEmpty = builder.Length == 0;
                        var varIsNullBuilder = new StringBuilder();
                        if (!rightExprEmpty)
                        {
                            varIsNullBuilder.Append('(');
                        }
                        if (userString)
                        {
                            RestrictedMemberSlot.MemberPath.ToCompactString(varIsNullBuilder, blockAlias);
                            varIsNullBuilder.Append(" is NULL");
                        }
                        else
                        {
                            RestrictedMemberSlot.MemberPath.AsEsql(varIsNullBuilder, blockAlias);
                            varIsNullBuilder.Append(" IS NULL");
                        }
                        if (!rightExprEmpty)
                        {
                            varIsNullBuilder.Append(" OR ");
                        }
                        builder.Insert(0, varIsNullBuilder.ToString());
                        if (!rightExprEmpty)
                        {
                            builder.Append(')');
                        }
                    },
                skipIsNotNull);

            inputBuilder.Append(builder);
            return inputBuilder;
        }

        private void AsCql(
            Action<NegatedConstant, IEnumerable<Constant>> negatedConstantAsCql,
            Action<Set<Constant>> varInDomain,
            Action varIsNotNull,
            Action varIsNull,
            bool skipIsNotNull)
        {
            Debug.Assert(RestrictedMemberSlot.MemberPath.IsScalarType(), "Expected scalar.");

            // If domain values contain a negated constant, delegate Cql generation into that constant.
            Debug.Assert(Domain.Values.Count(c => c is NegatedConstant) <= 1, "Multiple negated constants?");
            var negated = (NegatedConstant)Domain.Values.FirstOrDefault(c => c is NegatedConstant);
            if (negated != null)
            {
                negatedConstantAsCql(negated, Domain.Values);
            }
            else // We have only positive constants.
            {
                // 1. Generate "var in domain"
                // 2. If var is not nullable, append "... and var is not null". 
                //    This is needed for boolean _from variables that must never evaluate to null because view generation assumes 2-valued boolean logic.
                // 3. If domain contains null, prepend "var is null or ...".
                //
                // A complete generation pattern:
                //     (var is null or    ( var in domain    and var is not null))
                //      ^^^^^^^^^^^^^^      ^^^^^^^^^^^^^    ^^^^^^^^^^^^^^^^^^^
                //      generated by #3    generated by #1     generated by #2

                // Copy the domain values for simplification changes.
                var domainValues = new Set<Constant>(Domain.Values, Constant.EqualityComparer);

                var includeNull = false;
                if (domainValues.Contains(Constant.Null))
                {
                    includeNull = true;
                    domainValues.Remove(Constant.Null);
                }

                // Constraint counter-example could contain undefined cellconstant. E.g for booleans (for int its optimized out due to negated constants)
                // we want to treat undefined as nulls.
                if (domainValues.Contains(Constant.Undefined))
                {
                    includeNull = true;
                    domainValues.Remove(Constant.Undefined);
                }

                var excludeNull = !skipIsNotNull && RestrictedMemberSlot.MemberPath.IsNullable;

                Debug.Assert(!includeNull || !excludeNull, "includeNull and excludeNull can't be true at the same time.");

                // #1: Generate "var in domain"
                if (domainValues.Count > 0)
                {
                    varInDomain(domainValues);
                }

                // #2: Append "... and var is not null".
                if (excludeNull)
                {
                    varIsNotNull();
                }

                // #3: Prepend "var is null or ...".
                if (includeNull)
                {
                    varIsNull();
                }
            }
        }

        #endregion

        #region String methods

        internal override void ToCompactString(StringBuilder builder)
        {
            RestrictedMemberSlot.ToCompactString(builder);
            builder.Append(" IN (");
            StringUtil.ToCommaSeparatedStringSorted(builder, Domain.Values);
            builder.Append(")");
        }

        #endregion
    }
}
