using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
using System.Globalization;
using System.Diagnostics;
using EFIngresProvider.Helpers;

namespace EFIngresProvider.SqlGen
{
    partial class SqlGenerator
    {
        /// <summary>
        /// Dump out an expression - optionally wrap it with parantheses if possible
        /// </summary>
        /// <param name="e"></param>
        /// <param name="result"></param>
        private ISqlFragment ParanthesizeExpressionIfNeeded(DbExpression e)
        {
            if (IsComplexExpression(e))
            {
                return new SqlBuilder("(", e.Accept(this), ")");
            }
            else
            {
                return e.Accept(this);
            }
        }

        /// <summary>
        /// <see cref="CreateNewSelectStatement(SqlSelectStatement, string, TypeUsage, bool, out Symbol)"/>
        /// </summary>
        /// <param name="oldStatement"></param>
        /// <param name="inputVarName"></param>
        /// <param name="inputVarType"></param>
        /// <param name="fromSymbol"></param>
        /// <returns></returns>
        private SqlSelectStatement CreateNewSelectStatement(SqlSelectStatement oldStatement, string inputVarName, TypeUsage inputVarType, out Symbol fromSymbol)
        {
            return CreateNewSelectStatement(oldStatement, inputVarName, inputVarType, true, out fromSymbol);
        }

        /// <summary>
        /// This is called after a relational node's input has been visited, and the
        /// input's sql statement cannot be reused.  <see cref="Visit(DbProjectExpression)"/>
        ///
        /// When the input's sql statement cannot be reused, we create a new sql
        /// statement, with the old one as the from clause of the new statement.
        ///
        /// The old statement must be completed i.e. if it has an empty select list,
        /// the list of columns must be projected out.
        ///
        /// If the old statement being completed has a join symbol as its from extent,
        /// the new statement must have a clone of the join symbol as its extent.
        /// We cannot reuse the old symbol, but the new select statement must behave
        /// as though it is working over the "join" record.
        /// </summary>
        /// <param name="oldStatement"></param>
        /// <param name="inputVarName"></param>
        /// <param name="inputVarType"></param>
        /// <param name="finalizeOldStatement"></param>
        /// <param name="fromSymbol"></param>
        /// <returns>A new select statement, with the old one as the from clause.</returns>
        private SqlSelectStatement CreateNewSelectStatement(SqlSelectStatement oldStatement, string inputVarName, TypeUsage inputVarType, bool finalizeOldStatement, out Symbol fromSymbol)
        {
            fromSymbol = null;

            // Finalize the old statement
            if (finalizeOldStatement && oldStatement.Select.IsEmpty)
            {
                List<Symbol> columns = AddDefaultColumns(oldStatement);

                // Thid could not have been called from a join node.
                Debug.Assert(oldStatement.FromExtents.Count == 1);

                // if the oldStatement has a join as its input, ...
                // clone the join symbol, so that we "reuse" the
                // join symbol.  Normally, we create a new symbol - see the next block
                // of code.
                JoinSymbol oldJoinSymbol = oldStatement.FromExtents[0] as JoinSymbol;
                if (oldJoinSymbol != null)
                {
                    // Note: oldStatement.FromExtents will not do, since it might
                    // just be an alias of joinSymbol, and we want an actual JoinSymbol.
                    JoinSymbol newJoinSymbol = new JoinSymbol(inputVarName, inputVarType, oldJoinSymbol.ExtentList);
                    // This indicates that the oldStatement is a blocking scope
                    // i.e. it hides/renames extent columns
                    newJoinSymbol.IsNestedJoin = true;
                    newJoinSymbol.ColumnList = columns;
                    newJoinSymbol.FlattenedExtentList = oldJoinSymbol.FlattenedExtentList;

                    fromSymbol = newJoinSymbol;
                }
            }

            if (fromSymbol == null)
            {
                if (oldStatement.OutputColumnsRenamed)
                {
                    fromSymbol = new Symbol(inputVarName, inputVarType, oldStatement.OutputColumns);
                }
                else
                {
                    // This is just a simple extent/SqlSelectStatement,
                    // and we can get the column list from the type.
                    fromSymbol = new Symbol(inputVarName, inputVarType);
                }
            }

            // Observe that the following looks like the body of Visit(ExtentExpression).
            SqlSelectStatement selectStatement = new SqlSelectStatement();
            selectStatement.From.Append("( ");
            selectStatement.From.Append(oldStatement);
            selectStatement.From.AppendLine();
            selectStatement.From.Append(") ");


            return selectStatement;
        }

        /// <summary>
        /// Determine if the owner expression can add its unique sql to the input's
        /// SqlSelectStatement
        /// </summary>
        /// <param name="result">The SqlSelectStatement of the input to the relational node.</param>
        /// <param name="expressionKind">The kind of the expression node(not the input's)</param>
        /// <returns></returns>
        private static bool IsCompatible(SqlSelectStatement result, DbExpressionKind expressionKind)
        {
            switch (expressionKind)
            {
                case DbExpressionKind.Distinct:
                    return result.Top.IsEmpty
                        // The projection after distinct may not project all 
                        // columns used in the Order By
                        // Improvement: Consider getting rid of the Order By instead
                        && result.OrderBy.IsEmpty;

                case DbExpressionKind.Filter:
                    return result.Select.IsEmpty
                            && result.Where.IsEmpty
                            && result.GroupBy.IsEmpty
                            && result.Top.IsEmpty;

                case DbExpressionKind.GroupBy:
                    return result.Select.IsEmpty
                            && result.GroupBy.IsEmpty
                            && result.OrderBy.IsEmpty
                            && result.Top.IsEmpty;

                case DbExpressionKind.Limit:
                    return result.Top.TopCount == null;

                case DbExpressionKind.Element:
                    return result.Top.TopCount == null;

                case DbExpressionKind.Project:
                    // Allow a Project to be compatible with an OrderBy
                    // Otherwise we won't be able to sort an input, and project out only
                    // a subset of the input columns
                    return result.Select.IsEmpty
                            && result.GroupBy.IsEmpty
                        // If distinct is specified, the projection may affect
                        // the cardinality of the results, thus a new statement must be started.
                            && !result.IsDistinct;

                case DbExpressionKind.Skip:
                    return result.Top.SkipCount == null;

                case DbExpressionKind.Sort:
                    return result.Select.IsEmpty
                            && result.GroupBy.IsEmpty
                            && result.OrderBy.IsEmpty
                        // A Project may be on the top of the Sort, and if so, it would need
                        // to be in the same statement as the Sort (see comment above for the Project case).
                        // A Distinct in the same statement would prevent that, and therefore if Distinct is present,
                        // we need to start a new statement. 
                            && !result.IsDistinct;
                    //return result.OrderBy.IsEmpty;

                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException(String.Empty);
            }

        }

        /// <summary>
        /// We use double quotes (") for Ingres.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static string QuoteIdentifier(string name)
        {
            Debug.Assert(!String.IsNullOrEmpty(name));
            // We assume that the names are not quoted to begin with.
            return "\"" + name + "\"";
        }

        /// <summary>
        /// This is used to determine if a calling expression needs to place
        /// round brackets around the translation of the expression e.
        ///
        /// Constants, parameters and properties do not require brackets,
        /// everything else does.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>true, if the expression needs brackets </returns>
        private static bool IsComplexExpression(DbExpression e)
        {
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Constant:
                case DbExpressionKind.ParameterReference:
                case DbExpressionKind.Property:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Translates a list of SortClauses.
        /// Used in the translation of OrderBy 
        /// </summary>
        /// <param name="orderByClause">The SqlBuilder to which the sort keys should be appended</param>
        /// <param name="sortKeys"></param>
        private void AddSortKeys(SqlBuilder orderByClause, IList<DbSortClause> sortKeys)
        {
            string separator = "";
            foreach (DbSortClause sortClause in sortKeys)
            {
                orderByClause.Append(separator);
                orderByClause.Append(sortClause.Expression.Accept(this));

                // TODO: Is collate supported by Ingres?
                Debug.Assert(sortClause.Collation != null);
                if (!String.IsNullOrEmpty(sortClause.Collation))
                {
                    orderByClause.Append(" collate ");
                    orderByClause.Append(sortClause.Collation);
                }

                orderByClause.Append(sortClause.Ascending ? " asc" : " desc");

                separator = ", ";
            }
        }

        /// <summary>
        /// Gets escaped TSql identifier describing this entity set.
        /// </summary>
        /// <returns></returns>
        internal static string GetTargetTSql(EntitySetBase entitySetBase)
        {
            // construct escaped T-SQL referencing entity set
            StringBuilder builder = new StringBuilder(50);
            string definingQuery = MetadataHelpers.TryGetValueForMetadataProperty<string>(entitySetBase, "DefiningQuery");
            if (!string.IsNullOrEmpty(definingQuery))
            {
                builder.Append("(");
                builder.Append(definingQuery);
                builder.Append(")");
            }
            else
            {
                string schemaName = MetadataHelpers.TryGetValueForMetadataProperty<string>(entitySetBase, "Schema");
                if (!string.IsNullOrEmpty(schemaName))
                {
                    builder.Append(SqlGenerator.QuoteIdentifier(schemaName));
                    builder.Append(".");
                }
                else
                {
                    builder.Append(SqlGenerator.QuoteIdentifier(entitySetBase.EntityContainer.Name));
                    builder.Append(".");
                }

                string tableName = MetadataHelpers.TryGetValueForMetadataProperty<string>(entitySetBase, "Table");
                if (!string.IsNullOrEmpty(tableName))
                {
                    builder.Append(SqlGenerator.QuoteIdentifier(tableName));
                }
                else
                {
                    builder.Append(SqlGenerator.QuoteIdentifier(entitySetBase.Name));
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Handles the expression represending DbLimitExpression.Limit and DbSkipExpression.Count.
        /// If it is a constant expression, it simply does to string thus avoiding casting it to the specific value
        /// (which would be done if <see cref="Visit(DbConstantExpression)"/> is called)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment HandleCountExpression(DbExpression e)
        {
            if (e.ExpressionKind == DbExpressionKind.Constant)
            {
                //For constant expression we should not cast the value, 
                // thus we don't go throught the default DbConstantExpression handling
                return new SqlBuilder(((DbConstantExpression)e).Value.ToString());
            }
            else
            {
                return e.Accept(this);
            }
        }

        /// <summary>
        /// This is used to determine if a particular expression is an Apply operation.
        /// This is only the case when the DbExpressionKind is CrossApply or OuterApply.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        static bool IsApplyExpression(DbExpression e)
        {
            return (e.ExpressionKind == DbExpressionKind.CrossApply ||
                    e.ExpressionKind == DbExpressionKind.OuterApply);
        }

        /// <summary>
        /// This is used to determine if a particular expression is a Join operation.
        /// This is true for DbCrossJoinExpression and DbJoinExpression, the
        /// latter of which may have one of several different ExpressionKinds.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        static bool IsJoinExpression(DbExpression e)
        {
            return (e.ExpressionKind == DbExpressionKind.CrossJoin ||
                    e.ExpressionKind == DbExpressionKind.FullOuterJoin ||
                    e.ExpressionKind == DbExpressionKind.InnerJoin ||
                    e.ExpressionKind == DbExpressionKind.LeftOuterJoin);
        }

        static bool IsApplyOrJoinExpression(DbExpression e)
        {
            return IsApplyExpression(e) || IsJoinExpression(e);
        }


        private static readonly string[] _hexBytes = Enumerable.Range(0, 0x100).Select(x => string.Format("{0:X2}", x)).ToArray();

        private static string ByteArrayToBinaryString(Byte[] binaryArray)
        {
            return string.Concat(binaryArray.Select(x => _hexBytes[x]));
        }

        /// <summary>
        /// Helper method for the Group By visitor
        /// Returns true if at least one of the aggregates in the given list
        /// has an argument that is not a <see cref="DbPropertyExpression"/> 
        /// over <see cref="DbVariableReferenceExpression"/>
        /// </summary>
        /// <param name="aggregates"></param>
        /// <returns></returns>
        static bool NeedsInnerQuery(IList<DbAggregate> aggregates)
        {
            foreach (DbAggregate aggregate in aggregates)
            {
                Debug.Assert(aggregate.Arguments.Count == 1);
                if (!IsPropertyOverVarRef(aggregate.Arguments[0]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the given expression is a <see cref="DbPropertyExpression"/> 
        /// over <see cref="DbVariableReferenceExpression"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        static bool IsPropertyOverVarRef(DbExpression expression)
        {
            DbPropertyExpression propertyExpression = expression as DbPropertyExpression;
            if (propertyExpression == null)
            {
                return false;
            }
            DbVariableReferenceExpression varRefExpression = propertyExpression.Instance as DbVariableReferenceExpression;
            if (varRefExpression == null)
            {
                return false;
            }
            return true;
        }

        public static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        /// <summary>
        /// Is this a Store function (ie) does it have the builtinAttribute specified and it is not a canonical function?
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public bool IsBuiltInStoreFunction(EdmFunction function)
        {
            bool builtinAttribute = MetadataHelpers.TryGetValueForMetadataProperty<bool>(function, "BuiltInAttribute");
            return builtinAttribute && !MetadataHelpers.IsCanonicalFunction(function);
        }

        public void WriteFunctionName(SqlBuilder result, EdmFunction function)
        {
            string storeFunctionName = MetadataHelpers.TryGetValueForMetadataProperty<string>(function, "StoreFunctionNameAttribute");

            if (string.IsNullOrEmpty(storeFunctionName))
            {
                storeFunctionName = function.Name;
            }

            // If the function is a builtin (i.e. the BuiltIn attribute has been
            // specified, both store and canonical functions have this attribute), 
            // then the function name should not be quoted; 
            // additionally, no namespace should be used.
            if (MetadataHelpers.IsCanonicalFunction(function))
            {
                result.Append(storeFunctionName.ToUpperInvariant());
            }
            else if (IsBuiltInStoreFunction(function))
            {
                result.Append(storeFunctionName);
            }
            else
            {
                string schema = MetadataHelpers.TryGetValueForMetadataProperty<string>(function, "Schema");

                // Should we actually support this?
                if (String.IsNullOrEmpty(schema))
                {
                    result.Append(QuoteIdentifier(function.NamespaceName));
                }
                else
                {
                    result.Append(QuoteIdentifier(schema));
                }
                result.Append(".");
                result.Append(QuoteIdentifier(storeFunctionName));
            }
        }
    }
}
