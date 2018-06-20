using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common.CommandTrees;
using System.Diagnostics;
using EFIngresProvider.Helpers;
using System.Data.Metadata.Edm;
using EFIngresProvider.SqlGen.Functions;

namespace EFIngresProvider.SqlGen
{
    partial class SqlGenerator
    {
        public override ISqlFragment Visit(DbAndExpression e)
        {
            return VisitBinaryExpression("and", e.Left, e.Right);
        }

        public override ISqlFragment Visit(DbApplyExpression e)
        {
            throw new NotSupportedException("APPLY operator is not supported by Ingres.");
        }

        public override ISqlFragment Visit(DbArithmeticExpression e)
        {
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Plus:
                    return VisitBinaryExpression("+", e.Arguments[0], e.Arguments[1]);
                case DbExpressionKind.Minus:
                    return VisitBinaryExpression("-", e.Arguments[0], e.Arguments[1]);
                case DbExpressionKind.Multiply:
                    return VisitBinaryExpression("*", e.Arguments[0], e.Arguments[1]);
                case DbExpressionKind.Divide:
                    return VisitBinaryExpression("/", e.Arguments[0], e.Arguments[1]);
                case DbExpressionKind.Modulo:
                    return VisitFunction("mod", e.Arguments[0], e.Arguments[1]);
                case DbExpressionKind.UnaryMinus:
                    return new SqlBuilder(" -(", e.Arguments[0].Accept(this), ")");
                default:
                    throw new InvalidOperationException();
            }
        }

        public override ISqlFragment Visit(DbCaseExpression e)
        {
            Debug.Assert(e.When.Count == e.Then.Count);

            var result = new SqlBuilder();

            result.Append("case");
            for (int i = 0; i < e.When.Count; ++i)
            {
                result.Append(" when (", e.When[i].Accept(this), ") then ", e.Then[i].Accept(this));
            }
            if (e.Else != null)
            {
                result.Append(" else ", e.Else.Accept(this));
            }

            result.Append(" end");

            return result;
        }

        public override ISqlFragment Visit(DbCastExpression e)
        {
            return new SqlBuilder("cast(", e.Argument.Accept(this), " as ", MetadataHelpers.GetSqlPrimitiveType(e.ResultType), ")");
        }

        public override ISqlFragment Visit(DbComparisonExpression e)
        {
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Equals:
                    return VisitBinaryExpression("=", e.Left, e.Right);
                case DbExpressionKind.LessThan:
                    return VisitBinaryExpression("<", e.Left, e.Right);
                case DbExpressionKind.LessThanOrEquals:
                    return VisitBinaryExpression("<=", e.Left, e.Right);
                case DbExpressionKind.GreaterThan:
                    return VisitBinaryExpression(">", e.Left, e.Right);
                case DbExpressionKind.GreaterThanOrEquals:
                    return VisitBinaryExpression(">=", e.Left, e.Right);
                // The parser does not generate the expression kind below.
                case DbExpressionKind.NotEquals:
                    return VisitBinaryExpression("<>", e.Left, e.Right);
                default:
                    throw new InvalidOperationException(String.Empty);
            }
        }

        public override ISqlFragment Visit(DbConstantExpression e)
        {
            return VisitConstantExpression(e.ResultType, e.Value);
        }

        public override ISqlFragment Visit(DbCrossJoinExpression e)
        {
            return VisitJoinExpression(e.Inputs, e.ExpressionKind, "CROSS JOIN", null);
        }

        public override ISqlFragment Visit(DbDerefExpression e)
        {
            throw new NotSupportedException();
        }

        public override ISqlFragment Visit(DbDistinctExpression e)
        {
            var result = VisitExpressionEnsureSqlStatement(e.Argument);

            if (!IsCompatible(result, e.ExpressionKind))
            {
                Symbol fromSymbol;
                TypeUsage inputType = MetadataHelpers.GetElementTypeUsage(e.Argument.ResultType);
                result = CreateNewSelectStatement(result, "distinct", inputType, out fromSymbol);
                AddFromSymbol(result, "distinct", fromSymbol, false);
            }

            result.IsDistinct = true;
            return result;
        }

        public override ISqlFragment Visit(DbElementExpression e)
        {
            // ISSUE: What happens if the DbElementExpression is used as an input expression?
            // i.e. adding the '('  might not be right in all cases.
            return new SqlBuilder("(", VisitExpressionEnsureSqlStatement(e.Argument), ")");
        }

        public override ISqlFragment Visit(DbEntityRefExpression e)
        {
            throw new NotSupportedException();
        }

        public override ISqlFragment Visit(DbExceptExpression e)
        {
            throw new NotSupportedException();
        }

        public override ISqlFragment Visit(DbExpression e)
        {
            throw new InvalidOperationException();
        }

        public override ISqlFragment Visit(DbFilterExpression e)
        {
            return VisitFilterExpression(e.Input, e.Predicate, false);
        }

        /// <summary>
        /// Lambda functions are not supported.
        /// The functions supported are:
        /// <list type="number">
        /// <item>Canonical Functions - We recognize these by their dataspace, it is DataSpace.CSpace</item>
        /// <item>Store Functions - We recognize these by the BuiltInAttribute and not being Canonical</item>
        /// <item>User-defined Functions - All the rest except for Lambda functions</item>
        /// </list>
        /// We handle Canonical and Store functions the same way: If they are in the list of functions 
        /// that need special handling, we invoke the appropriate handler, otherwise we translate them to
        /// FunctionName(arg1, arg2, ..., argn).
        /// We translate user-defined functions to NamespaceName.FunctionName(arg1, arg2, ..., argn).
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbFunctionExpression e)
        {
            return CanonicalFunctions.Handle(this, e);
        }

        /// <summary>
        /// <see cref="Visit(DbFilterExpression)"/> for general details.
        /// We modify both the GroupBy and the Select fields of the SqlSelectStatement.
        /// GroupBy gets just the keys without aliases,
        /// and Select gets the keys and the aggregates with aliases.
        /// 
        /// Whenever there exists at least one aggregate with an argument that is not is not a simple
        /// <see cref="DbPropertyExpression"/>  over <see cref="DbVariableReferenceExpression"/>, 
        /// we create a nested query in which we alias the arguments to the aggregates. 
        /// That is due to the following two limitations of Sql Server:
        /// <list type="number">
        /// <item>If an expression being aggregated contains an outer reference, then that outer 
        /// reference must be the only column referenced in the expression </item>
        /// <item>Sql Server cannot perform an aggregate function on an expression containing 
        /// an aggregate or a subquery. </item>
        /// </list>
        /// 
        /// The default translation, without inner query is: 
        /// 
        ///     SELECT 
        ///         kexp1 AS key1, kexp2 AS key2,... kexpn AS keyn, 
        ///         aggf1(aexpr1) AS agg1, .. aggfn(aexprn) AS aggn
        ///     FROM input AS a
        ///     GROUP BY kexp1, kexp2, .. kexpn
        /// 
        /// When we inject an innner query, the equivalent translation is:
        /// 
        ///     SELECT 
        ///         key1 AS key1, key2 AS key2, .. keyn AS keys,  
        ///         aggf1(agg1) AS agg1, aggfn(aggn) AS aggn
        ///     FROM (
        ///             SELECT 
        ///                 kexp1 AS key1, kexp2 AS key2,... kexpn AS keyn, 
        ///                 aexpr1 AS agg1, .. aexprn AS aggn
        ///             FROM input AS a
        ///         ) as a
        ///     GROUP BY key1, key2, keyn
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/></returns>
        public override ISqlFragment Visit(DbGroupByExpression e)
        {
            Symbol fromSymbol;
            SqlSelectStatement innerQuery = VisitInputExpression(e.Input.Expression,
                e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            // GroupBy is compatible with Filter and OrderBy
            // but not with Project, GroupBy
            if (!IsCompatible(innerQuery, e.ExpressionKind))
            {
                innerQuery = CreateNewSelectStatement(innerQuery, e.Input.VariableName, e.Input.VariableType, out fromSymbol);
            }

            selectStatementStack.Push(innerQuery);
            symbolTable.EnterScope();

            AddFromSymbol(innerQuery, e.Input.VariableName, fromSymbol);
            // This line is not present for other relational nodes.
            symbolTable.Add(e.Input.GroupVariableName, fromSymbol);

            // The enumerator is shared by both the keys and the aggregates,
            // so, we do not close it in between.
            RowType groupByType = MetadataHelpers.GetEdmType<RowType>(MetadataHelpers.GetEdmType<CollectionType>(e.ResultType).TypeUsage);

            //Whenever there exists at least one aggregate with an argument that is not simply a PropertyExpression 
            // over a VarRefExpression, we need a nested query in which we alias the arguments to the aggregates.
            bool needsInnerQuery = NeedsInnerQuery(e.Aggregates);

            SqlSelectStatement result;
            if (needsInnerQuery)
            {
                //Create the inner query
                result = CreateNewSelectStatement(innerQuery, e.Input.VariableName, e.Input.VariableType, false, out fromSymbol);
                AddFromSymbol(result, e.Input.VariableName, fromSymbol, false);
            }
            else
            {
                result = innerQuery;
            }

            using (IEnumerator<EdmProperty> members = groupByType.Properties.GetEnumerator())
            {
                members.MoveNext();
                Debug.Assert(result.Select.IsEmpty);

                string separator = "";

                foreach (DbExpression key in e.Keys)
                {
                    EdmProperty member = members.Current;
                    var alias = new Symbol(member.Name);

                    result.GroupBy.Append(separator);

                    ISqlFragment keySql = key.Accept(this);

                    if (!needsInnerQuery)
                    {
                        //Default translation: Alias = Key
                        result.Select.Append(new SelectColumn(alias, keySql));
                        result.GroupBy.Append(keySql);
                    }
                    else
                    {
                        // The inner query contains the default translation Alias = Key
                        result.Select.Append(new SelectColumn(alias, keySql));

                        //The outer resulting query projects over the key aliased in the inner query: 
                        //  fromSymbol.Alias AS Alias
                        result.Select.Append(new SelectColumn(alias, fromSymbol, alias));
                        result.GroupBy.Append(alias);
                    }

                    separator = ", ";
                    members.MoveNext();
                }

                foreach (DbAggregate aggregate in e.Aggregates)
                {
                    EdmProperty member = members.Current;
                    var alias = new Symbol(member.Name);

                    Debug.Assert(aggregate.Arguments.Count == 1);
                    ISqlFragment translatedAggregateArgument = aggregate.Arguments[0].Accept(this);

                    ISqlFragment aggregateArgument;

                    if (needsInnerQuery)
                    {
                        //In this case the argument to the aggratete is reference to the one projected out by the
                        // inner query
                        SqlBuilder wrappingAggregateArgument = new SqlBuilder();
                        wrappingAggregateArgument.Append(fromSymbol);
                        wrappingAggregateArgument.Append(".");
                        wrappingAggregateArgument.Append(SqlGenerator.QuoteIdentifier(alias.Name));
                        aggregateArgument = wrappingAggregateArgument;

                        innerQuery.Select.Append(new SelectColumn(alias, translatedAggregateArgument));
                    }
                    else
                    {
                        aggregateArgument = translatedAggregateArgument;
                    }

                    ISqlFragment aggregateResult = VisitAggregate(aggregate, aggregateArgument);

                    result.Select.Append(new SelectColumn(alias, aggregateResult));

                    separator = ", ";
                    members.MoveNext();
                }
            }

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }

        public override ISqlFragment Visit(DbIntersectExpression e)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not(IsEmpty) has to be handled specially, so we delegate to
        /// <see cref="VisitIsEmptyExpression"/>.
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/>.
        /// <code>[NOT] EXISTS( ... )</code>
        /// </returns>
        public override ISqlFragment Visit(DbIsEmptyExpression e)
        {
            return VisitIsEmptyExpression(e, false);
        }

        public override ISqlFragment Visit(DbIsNullExpression e)
        {
            return VisitIsNullExpression(e, false);
        }

        public override ISqlFragment Visit(DbIsOfExpression e)
        {
            throw new NotSupportedException();
        }

        public override ISqlFragment Visit(DbJoinExpression e)
        {
            #region Map join type to a string
            string joinString;
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.FullOuterJoin:
                    joinString = "FULL OUTER JOIN";
                    break;

                case DbExpressionKind.InnerJoin:
                    joinString = "INNER JOIN";
                    break;

                case DbExpressionKind.LeftOuterJoin:
                    joinString = "LEFT OUTER JOIN";
                    break;

                default:
                    Debug.Assert(false);
                    joinString = null;
                    break;
            }
            #endregion

            var inputs = new List<DbExpressionBinding> { e.Left, e.Right };
            return VisitJoinExpression(inputs, e.ExpressionKind, joinString, e.JoinCondition);
        }

        public override ISqlFragment Visit(DbLikeExpression e)
        {
            var result = new SqlBuilder(e.Argument.Accept(this), " like ", e.Pattern.Accept(this));

            // if the ESCAPE expression is a DbNullExpression, then that's tantamount to 
            // not having an ESCAPE at all
            if (e.Escape.ExpressionKind != DbExpressionKind.Null)
            {
                result.Append(" escape ", e.Escape.Accept(this));
            }

            return result;
        }

        /// <summary>
        ///  Translates to TOP expression.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbLimitExpression e)
        {
            Debug.Assert(e.Limit is DbConstantExpression || e.Limit is DbParameterReferenceExpression, "DbLimitExpression.Limit is of invalid expression type");

            var result = VisitExpressionEnsureSqlStatement(e.Argument, false);

            if (!IsCompatible(result, e.ExpressionKind))
            {
                TypeUsage inputType = MetadataHelpers.GetElementTypeUsage(e.Argument.ResultType);

                Symbol fromSymbol;
                result = CreateNewSelectStatement(result, "first", inputType, out fromSymbol);
                AddFromSymbol(result, "first", fromSymbol, false);
            }

            result.Top.TopCount = HandleCountExpression(e.Limit);
            return result;
        }

        /// <summary>
        /// DbNewInstanceExpression is allowed as a child of DbProjectExpression only.
        /// If anyone else is the parent, we throw.
        /// We also perform special casing for collections - where we could convert
        /// them into Unions
        ///
        /// <see cref="VisitNewInstanceExpression"/> for the actual implementation.
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbNewInstanceExpression e)
        {
            if (MetadataHelpers.IsCollectionType(e.ResultType))
            {
                return VisitCollectionConstructor(e);
            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// The Not expression may cause the translation of its child to change.
        /// These children are
        /// <list type="bullet">
        /// <item><see cref="DbNotExpression"/>NOT(Not(x)) becomes x</item>
        /// <item><see cref="DbIsEmptyExpression"/>NOT EXISTS becomes EXISTS</item>
        /// <item><see cref="DbIsNullExpression"/>IS NULL becomes IS NOT NULL</item>
        /// <item><see cref="DbComparisonExpression"/>= becomes&lt;&gt; </item>
        /// </list>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbNotExpression e)
        {
            // Flatten Not(Not(x)) to x.
            if (e.Argument is DbNotExpression)
            {
                return ((DbNotExpression)e.Argument).Argument.Accept(this);
            }
            if (e.Argument is DbIsEmptyExpression)
            {
                return VisitIsEmptyExpression((DbIsEmptyExpression)e.Argument, true);
            }
            if (e.Argument is DbIsNullExpression)
            {
                return VisitIsNullExpression((DbIsNullExpression)e.Argument, true);
            }
            if (e.Argument is DbComparisonExpression)
            {
                var comparisonExpression = (DbComparisonExpression)e.Argument;
                if (comparisonExpression.ExpressionKind == DbExpressionKind.Equals)
                {
                    return VisitBinaryExpression("<>", comparisonExpression.Left, comparisonExpression.Right);
                }
            }
            return new SqlBuilder(" not (", e.Argument.Accept(this), ")");
        }

        public override ISqlFragment Visit(DbNullExpression e)
        {
            // always cast nulls - sqlserver doesn't like case expressions where the "then" clause is null
            return new SqlBuilder("cast(null as ", MetadataHelpers.GetSqlPrimitiveType(e.ResultType), ")");
        }

        public override ISqlFragment Visit(DbOfTypeExpression e)
        {
            throw new NotSupportedException();
        }

        public override ISqlFragment Visit(DbOrExpression e)
        {
            return VisitBinaryExpression("or", e.Left, e.Right);
        }

        public override ISqlFragment Visit(DbParameterReferenceExpression e)
        {
            // Do not quote this name.
            // We are not checking that e.Name has no illegal characters. e.g. space
            return new SqlBuilder("@", e.ParameterName);
        }

        /// <summary>
        /// <see cref="Visit(DbFilterExpression)"/> for the general ideas.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/></returns>
        /// <seealso cref="Visit(DbFilterExpression)"/>
        public override ISqlFragment Visit(DbProjectExpression e)
        {
            Symbol fromSymbol;
            var inputSelect = VisitInputExpression(e.Input.Expression, e.Input.VariableName, e.Input.VariableType, out fromSymbol);
            var select = inputSelect;

            // Project is compatible with Filter
            // but not with Project, GroupBy
            if (!IsCompatible(inputSelect, e.ExpressionKind))
            {
                select = CreateNewSelectStatement(inputSelect, e.Input.VariableName, e.Input.VariableType, out fromSymbol);
                select.Top.SetTopClause(inputSelect.Top);
                //select.OrderBy.Set(inputSelect.OrderBy);
                inputSelect.Top.Clear();
                //inputSelect.OrderBy.Clear();
            }

            selectStatementStack.Push(select);
            symbolTable.EnterScope();

            AddFromSymbol(select, e.Input.VariableName, fromSymbol);

            // Project is the only node that can have DbNewInstanceExpression as a child
            // so we have to check it here.
            // We call VisitNewInstanceExpression instead of Visit(DbNewInstanceExpression), since
            // the latter throws.
            if (e.Projection is DbNewInstanceExpression)
            {
                select.Select.Append(VisitNewInstanceExpression((DbNewInstanceExpression)e.Projection) as IEnumerable<SelectColumn>);
                if ((select != inputSelect) && (select.Select.Count == inputSelect.Select.Count))
                {
                    var newSelect = new SelectColumnList();
                    var inputColumns = inputSelect.Select.ToDictionary(x => x.Column.Name, x => x);
                    foreach (var column in select.Select)
                    {
                        if (!column.IsSimple)
                        {
                            break;
                        }
                        SelectColumn inputColumn;
                        if (inputColumns.TryGetValue(column.InnerName.Name, out inputColumn))
                        {
                            newSelect.Append(new SelectColumn(new Symbol(column.InnerName.Name), inputColumn.Expression));
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (inputColumns.Count == select.Select.Count)
                    {
                        inputSelect.Select = newSelect;
                        inputSelect.Top.SetTopClause(select.Top);
                        //inputSelect.OrderBy.Set(select.OrderBy);
                        select = inputSelect;
                    }
                }
            }
            else
            {
                select.Select.Append(e.Projection.Accept(this) as IEnumerable<SelectColumn>);
            }

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return select;
        }

        /// <summary>
        /// This method handles record flattening, which works as follows.
        /// consider an expression <c>Prop(y, Prop(x, Prop(d, Prop(c, Prop(b, Var(a)))))</c>
        /// where a,b,c are joins, d is an extent and x and y are fields.
        /// b has been flattened into a, and has its own SELECT statement.
        /// c has been flattened into b.
        /// d has been flattened into c.
        ///
        /// We visit the instance, so we reach Var(a) first.  This gives us a (join)symbol.
        /// Symbol(a).b gives us a join symbol, with a SELECT statement i.e. Symbol(b).
        /// From this point on , we need to remember Symbol(b) as the source alias,
        /// and then try to find the column.  So, we use a SymbolPair.
        ///
        /// We have reached the end when the symbol no longer points to a join symbol.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="JoinSymbol"/> if we have not reached the first
        /// Join node that has a SELECT statement.
        /// A <see cref="SymbolPair"/> if we have seen the JoinNode, and it has
        /// a SELECT statement.
        /// A <see cref="SqlBuilder"/> with {Input}.propertyName otherwise.
        /// </returns>
        public override ISqlFragment Visit(DbPropertyExpression e)
        {
            var instanceSql = e.Instance.Accept(this);

            // Since the DbVariableReferenceExpression is a proper child of ours, we can reset
            // isVarSingle.
            if (e.Instance is DbVariableReferenceExpression)
            {
                isVarRefSingle = false;
            }

            // We need to flatten, and have not yet seen the first nested SELECT statement.
            if (instanceSql is JoinSymbol)
            {
                var joinSymbol = (JoinSymbol)instanceSql;
                Debug.Assert(joinSymbol.NameToExtent.ContainsKey(e.Property.Name));
                if (joinSymbol.IsNestedJoin)
                {
                    return new SymbolPair(joinSymbol, joinSymbol.NameToExtent[e.Property.Name]);
                }
                else
                {
                    return joinSymbol.NameToExtent[e.Property.Name];
                }
            }

            // ---------------------------------------
            // We have seen the first nested SELECT statement, but not the column.
            if (instanceSql is SymbolPair)
            {
                SymbolPair symbolPair = (SymbolPair)instanceSql;
                if (symbolPair.Column is JoinSymbol)
                {
                    symbolPair.Column = ((JoinSymbol)symbolPair.Column).NameToExtent[e.Property.Name];
                    return symbolPair;
                }
                else
                {
                    // symbolPair.Column has the base extent.
                    // we need the symbol for the column, since it might have been renamed
                    // when handling a JOIN.
                    if (symbolPair.Column.Columns.ContainsKey(e.Property.Name))
                    {
                        return new SqlBuilder(symbolPair.Source, ".", symbolPair.Column.Columns[e.Property.Name]);
                    }
                }
            }
            // ---------------------------------------

            if (instanceSql is Symbol)
            {
                return new ColumnSymbol((Symbol)instanceSql, new Symbol(e.Property.Name, null));
            }

            // At this point the column name cannot be renamed, so we do
            // not use a symbol.
            return new SqlBuilder(instanceSql, ".", QuoteIdentifier(e.Property.Name));
        }

        /// <summary>
        /// Any(input, x) => Exists(Filter(input,x))
        /// All(input, x) => Not Exists(Filter(input, not(x))
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbQuantifierExpression e)
        {
            var result = new SqlBuilder();

            bool negatePredicate = (e.ExpressionKind == DbExpressionKind.All);
            if (e.ExpressionKind == DbExpressionKind.Any)
            {
                result.Append("exists (");
            }
            else
            {
                Debug.Assert(e.ExpressionKind == DbExpressionKind.All);
                result.Append("not exists (");
            }

            var filter = VisitFilterExpression(e.Input, e.Predicate, negatePredicate);
            if (filter.Select.IsEmpty)
            {
                AddDefaultColumns(filter);
            }

            result.Append(filter, ")");

            return result;
        }

        public override ISqlFragment Visit(DbRefExpression e)
        {
            throw new NotSupportedException();
        }

        public override ISqlFragment Visit(DbRefKeyExpression e)
        {
            throw new NotSupportedException();
        }

        public override ISqlFragment Visit(DbRelationshipNavigationExpression e)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns>If we are in a Join context, returns a <see cref="SqlBuilder"/>
        /// with the extent name, otherwise, a new <see cref="SqlSelectStatement"/>
        /// with the From field set.</returns>
        public override ISqlFragment Visit(DbScanExpression e)
        {
            if (IsParentAJoin)
            {
                return new SqlBuilder(GetTargetTSql(e.Target));
            }
            else
            {
                SqlSelectStatement result = new SqlSelectStatement();
                result.From.Append(GetTargetTSql(e.Target));
                return result;
            }
        }

        /// <summary>
        /// For Sql9 it translates to:
        /// SELECT Y.x1, Y.x2, ..., Y.xn
        /// FROM (
        ///     SELECT X.x1, X.x2, ..., X.xn, row_number() OVER (ORDER BY sk1, sk2, ...) AS [row_number] 
        ///     FROM input as X 
        ///     ) as Y
        /// WHERE Y.[row_number] > count 
        /// ORDER BY sk1, sk2, ...
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbSkipExpression e)
        {
            Debug.Assert(e.Count is DbConstantExpression || e.Count is DbParameterReferenceExpression, "DbSkipExpression.Count is of invalid expression type");

            Symbol fromSymbol;
            var result = VisitInputExpression(e.Input.Expression, e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            // Skip should be compatible with anything
            if (!IsCompatible(result, e.ExpressionKind))
            {
                result = CreateNewSelectStatement(result, e.Input.VariableName, e.Input.VariableType, out fromSymbol);
            }

            selectStatementStack.Push(result);
            symbolTable.EnterScope();

            AddFromSymbol(result, e.Input.VariableName, fromSymbol);

            AddSortKeys(result.OrderBy, e.SortOrder);

            result.Top.SkipCount = HandleCountExpression(e.Count);

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }

        /// <summary>
        /// <see cref="Visit(DbFilterExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/></returns>
        /// <seealso cref="Visit(DbFilterExpression)"/>
        public override ISqlFragment Visit(DbSortExpression e)
        {
            Symbol fromSymbol;
            var result = VisitInputExpression(e.Input.Expression, e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            // OrderBy is compatible with Filter and nothing else
            if (!IsCompatible(result, e.ExpressionKind))
            {
                result = CreateNewSelectStatement(result, e.Input.VariableName, e.Input.VariableType, out fromSymbol);
            }

            selectStatementStack.Push(result);
            symbolTable.EnterScope();

            AddFromSymbol(result, e.Input.VariableName, fromSymbol);

            AddSortKeys(result.OrderBy, e.SortOrder);

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }

        public override ISqlFragment Visit(DbTreatExpression e)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This code is shared by <see cref="Visit(DbExceptExpression)"/>
        /// and <see cref="Visit(DbIntersectExpression)"/>
        ///
        /// <see cref="VisitSetOpExpression"/>
        /// Since the left and right expression may not be Sql select statements,
        /// we must wrap them up to look like SQL select statements.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbUnionAllExpression e)
        {
            return VisitSetOpExpression(e.Left, e.Right, "UNION ALL");
        }

        /// <summary>
        /// This method determines whether an extent from an outer scope(free variable)
        /// is used in the CurrentSelectStatement.
        ///
        /// An extent in an outer scope, if its symbol is not in the FromExtents
        /// of the CurrentSelectStatement.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="Symbol"/>.</returns>
        public override ISqlFragment Visit(DbVariableReferenceExpression e)
        {
            if (isVarRefSingle)
            {
                throw new NotSupportedException();
                // A DbVariableReferenceExpression has to be a child of DbPropertyExpression or MethodExpression
                // This is also checked in GenerateSql(...) at the end of the visiting.
            }
            isVarRefSingle = true; // This will be reset by DbPropertyExpression or MethodExpression

            var result = symbolTable.Lookup(e.VariableName);
            if (!CurrentSelectStatement.FromExtents.Contains(result))
            {
                CurrentSelectStatement.OuterExtents[result] = true;
            }
            return result;
        }
    }
}
