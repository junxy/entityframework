// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServerCompact.SqlGen
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.SqlServerCompact.Resources;
    using System.Diagnostics;
    using System.Text;
    using BasicExpressionVisitor = System.Data.Entity.SqlServerCompact.BasicExpressionVisitor;

    /// <summary>
    ///     Class generating SQL for a DML command tree.
    /// </summary>
    internal static class DmlSqlGenerator
    {
        private const int s_commandTextBuilderInitialCapacity = 256;
        private static string storeGeneratedPatternString = "StoreGeneratedPattern";
        private static string rowversionString = "rowversion";

        /// <summary>
        ///     This method is added as a part of the fix for bug 13533
        ///     In this method we try to see from the command tree whether there is any 
        ///     updatable column(Property) available on the table(EntityType)
        private static bool GetUpdatableColumn(DbUpdateCommandTree tree, out string updatableColumnName)
        {
            var result = false;
            updatableColumnName = "";
            var entityType = (EntityType)tree.Target.VariableType.EdmType;

            foreach (var edmProperty in entityType.Properties)
            {
                if (entityType.KeyMembers.Contains(edmProperty.Name))
                {
                    // continue if it is a primary key
                    continue;
                }
                if (rowversionString == edmProperty.TypeUsage.EdmType.Name)
                {
                    // if the property is of type rowversion then we continue checking the next item in the list
                    continue;
                }

                // check whether the property is a identity type
                if (edmProperty.TypeUsage.Facets.Contains(storeGeneratedPatternString))
                {
                    var fct = edmProperty.TypeUsage.Facets.GetValue(storeGeneratedPatternString, false);
                    if (StoreGeneratedPattern.Identity
                        == (StoreGeneratedPattern)fct.Value)
                    {
                        // continue to check for the next property if the current property is a identity
                        continue;
                    }
                }
                //if the column is found then return the column name string
                updatableColumnName = edmProperty.Name;
                result = true;
                break;
            }

            return result;
        }

        internal static string[] GenerateUpdateSql(DbUpdateCommandTree tree, out List<DbParameter> parameters, bool isLocalProvider)
        {
            var commandTexts = new List<String>();
            var commandText = new StringBuilder(s_commandTextBuilderInitialCapacity);
            var translator = new ExpressionTranslator(commandText, tree, null != tree.Returning, isLocalProvider);

            // update [schemaName].[tableName]
            commandText.Append("update ");
            tree.Target.Expression.Accept(translator);
            commandText.AppendLine();

            // set c1 = ..., c2 = ..., ...
            var first = true;
            commandText.Append("set ");
            foreach (DbSetClause setClause in tree.SetClauses)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    commandText.Append(", ");
                }
                setClause.Property.Accept(translator);
                commandText.Append(" = ");
                setClause.Value.Accept(translator);
            }

            if (first)
            {
                // If first is still true, it indicates there were no set
                // clauses. 
                // - we acquire the appropriate locks
                // - server-gen columns (e.g. timestamp) get recomputed
                //

                // Bug fix #13533 : A fake update DML updating some column item 
                // with the same value as before to acquire the lock on the table 
                // while updating some columns in another table. This happens when
                // both the table are dependent on an entity and the members of entity
                // which is mapped to one table is being updated and the other table 
                // needs to be locked for consistancy.
                string updatableColumnName;
                if (GetUpdatableColumn(tree, out updatableColumnName))
                {
                    commandText.Append("[");
                    commandText.Append(CommonUtils.EscapeSquareBraceNames(updatableColumnName));
                    commandText.Append("] ");
                    commandText.Append(" = ");
                    commandText.Append("[");
                    commandText.Append(CommonUtils.EscapeSquareBraceNames(updatableColumnName));
                    commandText.Append("] ");
                }
                else
                {
                    // Throw some meaningful error
                    throw ADP1.Update(
                        EntityRes.GetString(EntityRes.UpdateStatementCannotBeGeneratedForAcquiringLock),
                        null);
                }
            }
            commandText.AppendLine();

            // where c1 = ..., c2 = ...
            commandText.Append("where ");
            tree.Predicate.Accept(translator);
            commandText.AppendLine();

            commandTexts.Add(commandText.ToString());
            commandText.Length = 0;

            // generate returning sql
            GenerateReturningSql(commandText, tree, translator, tree.Returning);

            if (!String.IsNullOrEmpty(commandText.ToString()))
            {
                commandTexts.Add(commandText.ToString());
            }

            parameters = translator.Parameters;

            return commandTexts.ToArray();
        }

        internal static string[] GenerateDeleteSql(DbDeleteCommandTree tree, out List<DbParameter> parameters, bool isLocalProvider)
        {
            var commandTexts = new List<String>();
            var commandText = new StringBuilder(s_commandTextBuilderInitialCapacity);
            var translator = new ExpressionTranslator(commandText, tree, false, isLocalProvider);

            // delete [schemaName].[tableName]
            commandText.Append("delete ");
            tree.Target.Expression.Accept(translator);
            commandText.AppendLine();

            // where c1 = ... AND c2 = ...
            commandText.Append("where ");
            tree.Predicate.Accept(translator);

            commandTexts.Add(commandText.ToString());
            commandText.Length = 0;

            parameters = translator.Parameters;
            return commandTexts.ToArray();
        }

        internal static string[] GenerateInsertSql(DbInsertCommandTree tree, out List<DbParameter> parameters, bool isLocalProvider)
        {
            var commandTexts = new List<String>();
            var commandText = new StringBuilder(s_commandTextBuilderInitialCapacity);
            var translator = new ExpressionTranslator(
                commandText, tree,
                null != tree.Returning, isLocalProvider);

            // insert [schemaName].[tableName]
            commandText.Append("insert ");
            tree.Target.Expression.Accept(translator);

            if (0 < tree.SetClauses.Count)
            {
                // (c1, c2, c3, ...)
                commandText.Append("(");
                var first = true;
                foreach (DbSetClause setClause in tree.SetClauses)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        commandText.Append(", ");
                    }
                    setClause.Property.Accept(translator);
                }
                commandText.AppendLine(")");

                // values c1, c2, ...
                first = true;
                commandText.Append("values (");
                foreach (DbSetClause setClause in tree.SetClauses)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        commandText.Append(", ");
                    }
                    setClause.Value.Accept(translator);

                    translator.RegisterMemberValue(setClause.Property, setClause.Value);
                }
                commandText.AppendLine(")");
            }
            else
            {
                // default values
                throw ADP1.NotSupported("Default values not supported");
            }

            commandTexts.Add(commandText.ToString());
            commandText.Length = 0;

            // generate returning sql
            GenerateReturningSql(commandText, tree, translator, tree.Returning);

            if (!String.IsNullOrEmpty(commandText.ToString()))
            {
                commandTexts.Add(commandText.ToString());
            }
            parameters = translator.Parameters;

            return commandTexts.ToArray();
        }

        // Generates T-SQL describing a member
        // Requires: member must belong to an entity type (a safe requirement for DML
        // SQL gen, where we only access table columns)
        private static string GenerateMemberTSql(EdmMember member)
        {
            // Don't check for cached sql belonging to this member
            // as methods are internal.
            //
            var entityType = (EntityType)member.DeclaringType;
            return SqlGenerator.QuoteIdentifier(member.Name);
        }

        /// <summary>
        ///     Generates SQL fragment returning server-generated values.
        ///     Requires: translator knows about member values so that we can figure out
        ///     how to construct the key predicate.
        ///     <code>Sample SQL:
        ///     
        ///         select IdentityValue
        ///         from MyTable
        ///         where IdentityValue = @@identity 
        ///     
        ///         NOTE: not scope_identity() because we don't support it.</code>
        /// </summary>
        /// <param name="commandText"> Builder containing command text </param>
        /// <param name="tree"> Modification command tree </param>
        /// <param name="translator"> Translator used to produce DML SQL statement for the tree </param>
        /// <param name="returning"> Returning expression. If null, the method returns immediately without producing a SELECT statement. </param>
        private static void GenerateReturningSql(
            StringBuilder commandText, DbModificationCommandTree tree,
            ExpressionTranslator translator, DbExpression returning)
        {
            if (returning != null)
            {
                commandText.Append("select ");
                returning.Accept(translator);
                commandText.AppendLine();
                commandText.Append("from ");
                tree.Target.Expression.Accept(translator);
                commandText.AppendLine();
                commandText.Append("where ");
                var target = ((DbScanExpression)tree.Target.Expression).Target;
                var flag = false;
                var isFirst = true;
                foreach (var member in target.ElementType.KeyMembers)
                {
                    DbParameter parameter;
                    if (!isFirst)
                    {
                        commandText.Append(" and ");
                    }
                    else
                    {
                        isFirst = false;
                    }

                    commandText.Append(GenerateMemberTSql(member));
                    commandText.Append(" = ");
                    if (translator.MemberValues.TryGetValue(member, out parameter))
                    {
                        commandText.Append(parameter.ParameterName);
                    }
                    else
                    {
                        if (flag)
                        {
                            throw ADP1.NotSupported(ADP1.Update_NotSupportedServerGenKey(target.Name));
                        }
                        if (!IsValidIdentityColumnType(member.TypeUsage))
                        {
                            throw ADP1.InvalidOperation(ADP1.Update_NotSupportedIdentityType(member.Name, member.TypeUsage.ToString()));
                        }
                        commandText.Append("@@IDENTITY");
                        flag = true;
                    }
                }
            }
        }

        private static bool IsValidIdentityColumnType(TypeUsage typeUsage)
        {
            // SQL CE supports the following types for identity columns:
            // int, bigint

            // make sure it's a primitive type
            if (typeUsage.EdmType.BuiltInTypeKind
                != BuiltInTypeKind.PrimitiveType)
            {
                return false;
            }

            // check if this is a supported primitive type (compare by name)
            var typeName = typeUsage.EdmType.Name;

            // integer types
            if (typeName == "int"
                || typeName == "bigint")
            {
                return true;
            }

            // type not in supported list
            return false;
        }

        /// <summary>
        ///     Lightweight expression translator for DML expression trees, which have constrained
        ///     scope and support.
        /// </summary>
        private class ExpressionTranslator : BasicExpressionVisitor
        {
            /// <summary>
            ///     Initialize a new expression translator populating the given string builder
            ///     with command text. Command text builder and command tree must not be null.
            /// </summary>
            /// <param name="commandText"> Command text with which to populate commands </param>
            /// <param name="commandTree"> Command tree generating SQL </param>
            /// <param name="preserveMemberValues"> Indicates whether the translator should preserve member values while compiling t-SQL (only needed for server generation) </param>
            internal ExpressionTranslator(
                StringBuilder commandText, DbModificationCommandTree commandTree,
                bool preserveMemberValues, bool isLocalProvider)
            {
                Debug.Assert(null != commandText);
                Debug.Assert(null != commandTree);
                _commandText = commandText;
                _commandTree = commandTree;
                _parameters = new List<DbParameter>();
                _memberValues = preserveMemberValues
                                    ? new Dictionary<EdmMember, DbParameter>()
                                    : null;
                _isLocalProvider = isLocalProvider;
            }

            private readonly StringBuilder _commandText;
            private readonly DbModificationCommandTree _commandTree;
            private readonly List<DbParameter> _parameters;
            private readonly Dictionary<EdmMember, DbParameter> _memberValues;
            private static readonly AliasGenerator _parameterNames = new AliasGenerator("@", 1000);
            private readonly bool _isLocalProvider;

            internal List<DbParameter> Parameters
            {
                get { return _parameters; }
            }

            internal Dictionary<EdmMember, DbParameter> MemberValues
            {
                get { return _memberValues; }
            }

            // generate parameter (name based on parameter ordinal)
            internal DbParameter CreateParameter(object value, TypeUsage type)
            {
                // Suppress the MaxLength facet in the type usage
                const bool ignoreMaxLengthFacet = true;
                var parameter = SqlCeProviderServices.CreateSqlCeParameter(
                    _parameterNames.GetName(_parameters.Count), type, value, ignoreMaxLengthFacet, _isLocalProvider);

                _parameters.Add(parameter);

                return parameter;
            }

            public override void Visit(DbAndExpression expression)
            {
                VisitBinary(expression, " and ");
            }

            public override void Visit(DbOrExpression expression)
            {
                VisitBinary(expression, " or ");
            }

            public override void Visit(DbComparisonExpression expression)
            {
                Debug.Assert(
                    expression.ExpressionKind == DbExpressionKind.Equals,
                    "only equals comparison expressions are produced in DML command trees in V1");

                VisitBinary(expression, " = ");

                RegisterMemberValue(expression.Left, expression.Right);
            }

            /// <summary>
            ///     Call this method to register a property value pair so the translator "remembers"
            ///     the values for members of the row being modified. These values can then be used
            ///     to form a predicate for server-generation (based on the key of the row)
            /// </summary>
            /// <param name="propertyExpression"> DbExpression containing the column reference (property expression). </param>
            /// <param name="value"> DbExpression containing the value of the column. </param>
            internal void RegisterMemberValue(DbExpression propertyExpression, DbExpression value)
            {
                if (null != _memberValues)
                {
                    // register the value for this property
                    Debug.Assert(
                        propertyExpression.ExpressionKind == DbExpressionKind.Property,
                        "DML predicates and setters must be of the form property = value");

                    // get name of left property 
                    var property = ((DbPropertyExpression)propertyExpression).Property;

                    // don't track null values
                    if (value.ExpressionKind
                        != DbExpressionKind.Null)
                    {
                        Debug.Assert(
                            value.ExpressionKind == DbExpressionKind.Constant,
                            "value must either constant or null");
                        // retrieve the last parameter added (which describes the parameter)
                        _memberValues[property] = _parameters[_parameters.Count - 1];
                    }
                }
            }

            public override void Visit(DbIsNullExpression expression)
            {
                expression.Argument.Accept(this);
                _commandText.Append(" is null");
            }

            public override void Visit(DbNotExpression expression)
            {
                _commandText.Append("not (");
                expression.Accept(this);
                _commandText.Append(")");
            }

            public override void Visit(DbConstantExpression expression)
            {
                var parameter = CreateParameter(expression.Value, expression.ResultType);
                _commandText.Append(parameter.ParameterName);
            }

            public override void Visit(DbScanExpression expression)
            {
                // we know we won't hit this code unless there is no function defined for this
                // ModificationOperation, so if this EntitySet is using a DefiningQuery, instead
                // of a table, that is an error
                MetadataProperty definingQuery;

                if (expression.Target.MetadataProperties.TryGetValue("DefiningQuery", false, out definingQuery)
                    &&
                    null != definingQuery.Value)
                {
                    string missingCudElement;
                    var ict = _commandTree as DbInsertCommandTree;
                    var dct = _commandTree as DbDeleteCommandTree;
#if DEBUG
                    var uct = _commandTree as DbUpdateCommandTree;
#endif

                    if (null != dct)
                    {
                        missingCudElement = "DeleteFunction" /*StorageMslConstructs.DeleteFunctionElement*/;
                    }
                    else if (null != ict)
                    {
                        missingCudElement = "InsertFunction" /*StorageMslConstructs.InsertFunctionElement*/;
                    }
                    else
                    {
#if DEBUG
                        Debug.Assert(null != uct, "did you add a new option?");
#endif

                        missingCudElement = "UpdateFunction" /*StorageMslConstructs.UpdateFunctionElement*/;
                    }
                    throw ADP1.Update(
                        EntityRes.GetString(
                            EntityRes.Update_SqlEntitySetWithoutDmlFunctions,
                            expression.Target.Name,
                            missingCudElement,
                            "ModificationFunctionMapping" /*StorageMslConstructs.ModificationFunctionMappingElement*/),
                        null);
                }

                _commandText.Append(SqlGenerator.GetTargetTSql(expression.Target));
            }

            public override void Visit(DbPropertyExpression expression)
            {
                _commandText.Append(GenerateMemberTSql(expression.Property));
            }

            public override void Visit(DbNullExpression expression)
            {
                _commandText.Append("null");
            }

            public override void Visit(DbNewInstanceExpression expression)
            {
                // assumes all arguments are self-describing (no need to use aliases
                // because no renames are ever used in the projection)
                var first = true;
                foreach (var argument in expression.Arguments)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        _commandText.Append(", ");
                    }
                    argument.Accept(this);
                }
            }

            private void VisitBinary(DbBinaryExpression expression, string separator)
            {
                _commandText.Append("(");
                expression.Left.Accept(this);
                _commandText.Append(separator);
                expression.Right.Accept(this);
                _commandText.Append(")");
            }
        }
    }
}
