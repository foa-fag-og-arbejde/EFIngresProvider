using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;
using EFIngresProvider.Helpers;
using System.Diagnostics;

namespace EFIngresProvider.SqlGen
{
    partial class SqlGenerator
    {
        /// <summary>
        /// Aggregates are not visited by the normal visitor walk.
        /// </summary>
        /// <param name="aggregate">The aggreate go be translated</param>
        /// <param name="aggregateArgument">The translated aggregate argument</param>
        /// <returns></returns>
        private SqlBuilder VisitAggregate(DbAggregate aggregate, ISqlFragment aggregateArgument)
        {
            SqlBuilder aggregateResult = new SqlBuilder();
            DbFunctionAggregate functionAggregate = aggregate as DbFunctionAggregate;

            if (functionAggregate == null)
            {
                throw new NotSupportedException();
            }

            //The only aggregate function with different name is Big_Count
            //Note: If another such function is to be added, a dictionary should be created
            if (MetadataHelpers.IsCanonicalFunction(functionAggregate.Function)
                && String.Equals(functionAggregate.Function.Name, "BigCount", StringComparison.Ordinal))
            {
                aggregateResult.Append("COUNT_BIG");
            }
            else
            {
                WriteFunctionName(aggregateResult, functionAggregate.Function);
            }

            aggregateResult.Append("(");

            DbFunctionAggregate fnAggr = functionAggregate;
            if ((null != fnAggr) && (fnAggr.Distinct))
            {
                aggregateResult.Append("DISTINCT ");
            }

            aggregateResult.Append(aggregateArgument);

            aggregateResult.Append(")");
            return aggregateResult;
        }

        private ISqlFragment VisitFunction(string function, params DbExpression[] arguments)
        {
            var result = new SqlBuilder();
            result.Append(function, "(");
            var first = true;
            foreach (var argument in arguments)
            {
                if (!first)
                {
                    result.Append(", ");
                }
                result.Append(ParanthesizeExpressionIfNeeded(argument));
                first = false;
            }
            result.Append(")");
            return result;
        }

        private ISqlFragment VisitUnaryExpression(string op, DbExpression arg)
        {
            return new SqlBuilder(op, " ", ParanthesizeExpressionIfNeeded(arg));
        }

        private ISqlFragment VisitBinaryExpression(string op, DbExpression left, DbExpression right)
        {
            return new SqlBuilder(ParanthesizeExpressionIfNeeded(left), " ", op, " ", ParanthesizeExpressionIfNeeded(right));
        }

        public ISqlFragment VisitConstantExpression(TypeUsage type, object value)
        {
            PrimitiveTypeKind typeKind;
            // Model Types can be (at the time of this implementation):
            //      Binary, Boolean, Byte, DateTime, Decimal, Double, Guid, Int16, Int32, Int64, Single, String
            if (MetadataHelpers.TryGetPrimitiveTypeKind(type, out typeKind))
            {
                return VisitConstantExpression(typeKind, value);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private string FormatDateTime(DateTime value)
        {
            var ingresDate = IngresDate.Create(value);
            if (ingresDate.IngresDateKind == IngresDateKind.DateTime)
            {
                var localValue = ingresDate.Kind == DateTimeKind.Utc ? ingresDate.ToLocalTime() : ingresDate;
                if (localValue.IsDate)
                {
                    return IngresDate.Format(localValue);
                }
            }
            return IngresDate.Format(ingresDate);
        }

        private string FormatTimeSpan(TimeSpan value)
        {
            return IngresDate.Format(IngresDate.Create(value));
        }

        public ISqlFragment VisitConstantExpression(PrimitiveTypeKind typeKind, object value)
        {
            // Model Types can be (at the time of this implementation):
            //      Binary, Boolean, Byte, DateTime, Decimal, Double, Guid, Int16, Int32, Int64, Single, String
            switch (typeKind)
            {
                case PrimitiveTypeKind.Binary:
                    return new SqlBuilder(Format("X'{0}'", ByteArrayToBinaryString((Byte[])value)));

                case PrimitiveTypeKind.Boolean:
                    return new SqlBuilder((bool)value ? "int1(1)" : "int1(0)");

                case PrimitiveTypeKind.Byte:
                    return new SqlBuilder(Format("int1({0})", value));

                case PrimitiveTypeKind.SByte:
                    return new SqlBuilder(Format("int1({0})", value));

                case PrimitiveTypeKind.Int16:
                    return new SqlBuilder(Format("int2({0})", value));

                case PrimitiveTypeKind.Int32:
                    return new SqlBuilder(Format("int4({0})", value));

                case PrimitiveTypeKind.Int64:
                    return new SqlBuilder(Format("int8({0})", value));

                case PrimitiveTypeKind.DateTime:
                    return new SqlBuilder(FormatDateTime((DateTime)value));

                case PrimitiveTypeKind.Time:
                    return new SqlBuilder(FormatTimeSpan((TimeSpan)value));

                case PrimitiveTypeKind.Decimal:
                    var strDecimal = Format("{0}", value);
                    var precision = (byte)strDecimal.Length;
                    if (strDecimal.StartsWith("-"))
                    {
                        precision -= 1;
                    }
                    var scale = (byte)0;
                    var i = strDecimal.IndexOf('.');
                    if (i >= 0)
                    {
                        precision -= 1;
                        scale = (byte)(strDecimal.Length - i - 1);
                    }
                    return new SqlBuilder(Format("decimal({0}, {1}, {2})", strDecimal, precision, scale));

                case PrimitiveTypeKind.Double:
                    return new SqlBuilder(Format("float8({0:R})", value));

                case PrimitiveTypeKind.Single:
                    return new SqlBuilder(Format("float4({0:R})", value));

                case PrimitiveTypeKind.String:
                    return new SqlBuilder(Format("'{0}'", ((string)value).Replace("'", "''")));

                default:
                    // all known scalar types should been handled already.
                    throw new NotSupportedException("Primitive type kind " + typeKind + " is not supported by the Sample Provider");
            }
        }

        /// <summary>
        /// Translate a NewInstance(Element(X)) expression into
        ///   "select top(1) * from X"
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment VisitCollectionConstructor(DbNewInstanceExpression e)
        {
            Debug.Assert(e.Arguments.Count <= 1);

            if (e.Arguments.Count == 1 && e.Arguments[0].ExpressionKind == DbExpressionKind.Element)
            {
                DbElementExpression elementExpr = e.Arguments[0] as DbElementExpression;
                SqlSelectStatement result = VisitExpressionEnsureSqlStatement(elementExpr.Argument);

                if (!IsCompatible(result, DbExpressionKind.Element))
                {
                    Symbol fromSymbol;
                    TypeUsage inputType = MetadataHelpers.GetElementTypeUsage(elementExpr.Argument.ResultType);

                    result = CreateNewSelectStatement(result, "element", inputType, out fromSymbol);
                    AddFromSymbol(result, "element", fromSymbol, false);
                }
                result.Top.SetTopCount(1);
                return result;
            }


            // Otherwise simply build this out as a union-all ladder
            CollectionType collectionType = MetadataHelpers.GetEdmType<CollectionType>(e.ResultType);
            Debug.Assert(collectionType != null);
            bool isScalarElement = MetadataHelpers.IsPrimitiveType(collectionType.TypeUsage);

            SqlBuilder resultSql = new SqlBuilder();
            string separator = "";

            // handle empty table
            if (e.Arguments.Count == 0)
            {
                Debug.Assert(isScalarElement);
                resultSql.Append(" select cast(null as ");
                resultSql.Append(MetadataHelpers.GetSqlPrimitiveType(collectionType.TypeUsage));
                resultSql.Append(") as x from (select 1) as y where 1=0");
            }

            foreach (DbExpression arg in e.Arguments)
            {
                resultSql.Append(separator);
                resultSql.Append(" select ");
                resultSql.Append(arg.Accept(this));
                // For scalar elements, no alias is appended yet. Add this.
                if (isScalarElement)
                {
                    resultSql.Append(" as x ");
                }
                separator = " union all ";
            }

            return resultSql;
        }

        /// <summary>
        /// <see cref="Visit(DbIsNullExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="negate">Was the parent a DbNotExpression?</param>
        /// <returns></returns>
        private SqlBuilder VisitIsNullExpression(DbIsNullExpression e, bool negate)
        {
            if (!negate)
            {
                return new SqlBuilder(e.Argument.Accept(this), " is null");
            }
            else
            {
                return new SqlBuilder(e.Argument.Accept(this), " is not null");
            }
        }

        /// <summary>
        /// <see cref="Visit(DbIsEmptyExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="negate">Was the parent a DbNotExpression?</param>
        /// <returns></returns>
        SqlBuilder VisitIsEmptyExpression(DbIsEmptyExpression e, bool negate)
        {
            if (negate)
            {
                return new SqlBuilder(" exists (", VisitExpressionEnsureSqlStatement(e.Argument), Environment.NewLine, ")");
            }
            else
            {
                return new SqlBuilder(" not exists (", VisitExpressionEnsureSqlStatement(e.Argument), Environment.NewLine, ")");
            }
        }

        /// <summary>
        /// We assume that this is only called as a child of a Project.
        ///
        /// This replaces <see cref="Visit(DbNewInstanceExpression)"/>, since
        /// we do not allow DbNewInstanceExpression as a child of any node other than
        /// DbProjectExpression.
        ///
        /// We write out the translation of each of the columns in the record.
        /// </summary>
        /// <param name="e"></param>

        /// <returns>A <see cref="SqlBuilder"/></returns>
        ISqlFragment VisitNewInstanceExpression(DbNewInstanceExpression e)
        {
            var result = new SelectColumnList();
            var rowType = e.ResultType.EdmType as RowType;

            if (rowType != null)
            {
                var members = rowType.Properties;
                for (int i = 0; i < e.Arguments.Count; ++i)
                {
                    var argument = e.Arguments[i];
                    if (MetadataHelpers.IsRowType(argument.ResultType))
                    {
                        // We do not support nested records or other complex objects.
                        throw new NotSupportedException();
                    }

                    EdmProperty member = members[i];
                    var expression = argument.Accept(this);
                    var columnSymbol = expression as ColumnSymbol;
                    if (columnSymbol != null)
                    {
                        result.Append(new SelectColumn(new Symbol(member.Name, member.TypeUsage), columnSymbol.Source, columnSymbol.Column));
                    }
                    else
                    {
                        result.Append(new SelectColumn(new Symbol(member.Name, member.TypeUsage), expression));
                    }
                }
            }
            else
            {
                //
                // Types other then RowType (such as UDTs for instance) are not supported.
                //
                throw new NotSupportedException();
            }

            return result;
        }

        /// <summary>
        /// Simply calls <see cref="VisitExpressionEnsureSqlStatement(DbExpression, bool)"/>
        /// with addDefaultColumns set to true
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private SqlSelectStatement VisitExpressionEnsureSqlStatement(DbExpression e)
        {
            return VisitExpressionEnsureSqlStatement(e, true);
        }

        /// <summary>
        /// This is called from <see cref="GenerateSql(DbQueryCommandTree)"/> and nodes which require a
        /// select statement as an argument e.g. <see cref="Visit(DbIsEmptyExpression)"/>,
        /// <see cref="Visit(DbUnionAllExpression)"/>.
        ///
        /// SqlGenerator needs its child to have a proper alias if the child is
        /// just an extent or a join.
        ///
        /// The normal relational nodes result in complete valid SQL statements.
        /// For the rest, we need to treat them as there was a dummy
        /// <code>
        /// -- originally {expression}
        /// -- change that to
        /// SELECT *
        /// FROM {expression} as c
        /// </code>
        /// 
        /// DbLimitExpression needs to start the statement but not add the default columns
        /// </summary>
        /// <param name="e"></param>
        /// <param name="addDefaultColumns"></param>
        /// <returns></returns>
        private SqlSelectStatement VisitExpressionEnsureSqlStatement(DbExpression e, bool addDefaultColumns)
        {
            Debug.Assert(MetadataHelpers.IsCollectionType(e.ResultType.EdmType));

            SqlSelectStatement result;
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Project:
                case DbExpressionKind.Filter:
                case DbExpressionKind.GroupBy:
                case DbExpressionKind.Sort:
                    result = e.Accept(this) as SqlSelectStatement;
                    break;

                default:
                    string inputVarName = "c";  // any name will do - this is my random choice.
                    symbolTable.EnterScope();

                    TypeUsage type = null;
                    switch (e.ExpressionKind)
                    {
                        case DbExpressionKind.Scan:
                        case DbExpressionKind.CrossJoin:
                        case DbExpressionKind.FullOuterJoin:
                        case DbExpressionKind.InnerJoin:
                        case DbExpressionKind.LeftOuterJoin:
                        case DbExpressionKind.CrossApply:
                        case DbExpressionKind.OuterApply:
                            // It used to be type = e.ResultType. 
                            type = MetadataHelpers.GetElementTypeUsage(e.ResultType);
                            break;

                        default:
                            Debug.Assert(MetadataHelpers.IsCollectionType(e.ResultType.EdmType));
                            type = MetadataHelpers.GetEdmType<CollectionType>(e.ResultType).TypeUsage;
                            break;
                    }

                    Symbol fromSymbol;
                    result = VisitInputExpression(e, inputVarName, type, out fromSymbol);
                    AddFromSymbol(result, inputVarName, fromSymbol);
                    symbolTable.ExitScope();
                    break;
            }

            if (addDefaultColumns && result.Select.IsEmpty)
            {
                AddDefaultColumns(result);
            }

            return result;
        }

        /// <summary>
        /// This method is called by <see cref="Visit(DbFilterExpression)"/> and
        /// <see cref="Visit(DbQuantifierExpression)"/>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <param name="negatePredicate">This is passed from <see cref="Visit(DbQuantifierExpression)"/>
        /// in the All(...) case.</param>
        /// <returns></returns>
        private SqlSelectStatement VisitFilterExpression(DbExpressionBinding input, DbExpression predicate, bool negatePredicate)
        {
            Symbol fromSymbol;
            var result = VisitInputExpression(input.Expression, input.VariableName, input.VariableType, out fromSymbol);

            // Filter is compatible with OrderBy
            // but not with Project, another Filter or GroupBy
            if (!IsCompatible(result, DbExpressionKind.Filter))
            {
                result = CreateNewSelectStatement(result, input.VariableName, input.VariableType, out fromSymbol);
            }

            selectStatementStack.Push(result);
            symbolTable.EnterScope();

            AddFromSymbol(result, input.VariableName, fromSymbol);

            if (negatePredicate)
            {
                result.Where.Append("NOT (");
            }
            result.Where.Append(predicate.Accept(this));
            if (negatePredicate)
            {
                result.Where.Append(")");
            }

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }

        /// <summary>
        /// This is called by the relational nodes.  It does the following
        /// <list>
        /// <item>If the input is not a SqlSelectStatement, it assumes that the input
        /// is a collection expression, and creates a new SqlSelectStatement </item>
        /// </list>
        /// </summary>
        /// <param name="inputExpression"></param>
        /// <param name="inputVarName"></param>
        /// <param name="inputVarType"></param>
        /// <param name="fromSymbol"></param>
        /// <returns>A <see cref="SqlSelectStatement"/> and the main fromSymbol
        /// for this select statement.</returns>
        private SqlSelectStatement VisitInputExpression(DbExpression inputExpression, string inputVarName, TypeUsage inputVarType, out Symbol fromSymbol)
        {
            SqlSelectStatement result;
            ISqlFragment sqlFragment = inputExpression.Accept(this);
            result = sqlFragment as SqlSelectStatement;

            if (result == null)
            {
                result = new SqlSelectStatement();
                WrapNonQueryExtent(result, sqlFragment, inputExpression.ExpressionKind);
            }

            if (result.FromExtents.Count == 0)
            {
                // input was an extent
                fromSymbol = new Symbol(inputVarName, inputVarType);
            }
            else if (result.FromExtents.Count == 1)
            {
                // input was Filter/GroupBy/Project/OrderBy
                // we are likely to reuse this statement.
                fromSymbol = result.FromExtents[0];
            }
            else
            {
                // input was a join.
                // we are reusing the select statement produced by a Join node
                // we need to remove the original extents, and replace them with a
                // new extent with just the Join symbol.
                JoinSymbol joinSymbol = new JoinSymbol(inputVarName, inputVarType, result.FromExtents);
                joinSymbol.FlattenedExtentList = result.AllJoinExtents;

                fromSymbol = joinSymbol;
                result.FromExtents.Clear();
                result.FromExtents.Add(fromSymbol);
            }

            return result;
        }

        /// <summary>
        /// This handles the processing of join expressions.
        /// The extents on a left spine are flattened, while joins
        /// not on the left spine give rise to new nested sub queries.
        ///
        /// Joins work differently from the rest of the visiting, in that
        /// the parent (i.e. the join node) creates the SqlSelectStatement
        /// for the children to use.
        ///
        /// The "parameter" IsInJoinContext indicates whether a child extent should
        /// add its stuff to the existing SqlSelectStatement, or create a new SqlSelectStatement
        /// By passing true, we ask the children to add themselves to the parent join,
        /// by passing false, we ask the children to create new Select statements for
        /// themselves.
        ///
        /// This method is called from <see cref="Visit(DbApplyExpression)"/> and
        /// <see cref="Visit(DbJoinExpression)"/>.
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="joinKind"></param>
        /// <param name="joinString"></param>
        /// <param name="joinCondition"></param>
        /// <returns> A <see cref="SqlSelectStatement"/></returns>
        private ISqlFragment VisitJoinExpression(IList<DbExpressionBinding> inputs, DbExpressionKind joinKind, string joinString, DbExpression joinCondition)
        {
            // If the parent is not a join( or says that it is not),
            // we should create a new SqlSelectStatement.
            // otherwise, we add our child extents to the parent's FROM clause.
            if (!IsParentAJoin)
            {
                selectStatementStack.Push(new SqlSelectStatement { AllJoinExtents = new List<Symbol>() });
            }
            var result = CurrentSelectStatement;

            // Process each of the inputs, and then the joinCondition if it exists.
            // It would be nice if we could call VisitInputExpression - that would
            // avoid some code duplication
            // but the Join postprocessing is messy and prevents this reuse.
            symbolTable.EnterScope();

            var separator = "";
            var isLeftMostInput = true;
            var inputCount = inputs.Count;
            foreach (var input in Enumerable.Range(0, inputs.Count).Select(x => inputs[x]))
            {
                if (separator.Length != 0)
                {
                    result.From.AppendLine();
                }
                result.From.Append(separator + " ");
                // Change this if other conditions are required
                // to force the child to produce a nested SqlStatement.
                var needsJoinContext = input.Expression.ExpressionKind == DbExpressionKind.Scan ||
                                       (isLeftMostInput && IsApplyOrJoinExpression(input.Expression));

                isParentAJoinStack.Push(needsJoinContext ? true : false);

                // if the child reuses our select statement, it will append the from
                // symbols to our FromExtents list.  So, we need to remember the
                // start of the child's entries.
                var fromSymbolStart = result.FromExtents.Count;
                var fromExtentFragment = input.Expression.Accept(this);

                isParentAJoinStack.Pop();

                ProcessJoinInputResult(fromExtentFragment, result, input, fromSymbolStart);
                separator = joinString;

                isLeftMostInput = false;
            }

            // Visit the on clause/join condition.
            switch (joinKind)
            {
                case DbExpressionKind.FullOuterJoin:
                case DbExpressionKind.InnerJoin:
                case DbExpressionKind.LeftOuterJoin:
                    result.From.Append(" on ");
                    isParentAJoinStack.Push(false);
                    result.From.Append(joinCondition.Accept(this));
                    isParentAJoinStack.Pop();
                    break;
            }

            symbolTable.ExitScope();

            if (!IsParentAJoin)
            {
                selectStatementStack.Pop();
            }

            return result;
        }

        /// <summary>
        /// This is called from <see cref="VisitJoinExpression"/>.
        ///
        /// This is responsible for maintaining the symbol table after visiting
        /// a child of a join expression.
        ///
        /// The child's sql statement may need to be completed.
        ///
        /// The child's result could be one of
        /// <list type="number">
        /// <item>The same as the parent's - this is treated specially.</item>
        /// <item>A sql select statement, which may need to be completed</item>
        /// <item>An extent - just copy it to the from clause</item>
        /// <item>Anything else (from a collection-valued expression) -
        /// unnest and copy it.</item>
        /// </list>
        ///
        /// If the input was a Join, we need to create a new join symbol,
        /// otherwise, we create a normal symbol.
        ///
        /// We then call AddFromSymbol to add the AS clause, and update the symbol table.
        ///
        ///
        ///
        /// If the child's result was the same as the parent's, we have to clean up
        /// the list of symbols in the FromExtents list, since this contains symbols from
        /// the children of both the parent and the child.
        /// The happens when the child visited is a Join, and is the leftmost child of
        /// the parent.
        /// </summary>
        /// <param name="fromExtentFragment"></param>
        /// <param name="result"></param>
        /// <param name="input"></param>
        /// <param name="fromSymbolStart"></param>
        private void ProcessJoinInputResult(ISqlFragment fromExtentFragment, SqlSelectStatement result, DbExpressionBinding input, int fromSymbolStart)
        {
            Symbol fromSymbol = null;

            if (result != fromExtentFragment)
            {
                // The child has its own select statement, and is not reusing
                // our select statement.
                // This should look a lot like VisitInputExpression().
                SqlSelectStatement sqlSelectStatement = fromExtentFragment as SqlSelectStatement;
                if (sqlSelectStatement != null)
                {
                    if (sqlSelectStatement.Select.IsEmpty)
                    {
                        List<Symbol> columns = AddDefaultColumns(sqlSelectStatement);

                        if (IsJoinExpression(input.Expression)
                            || IsApplyExpression(input.Expression))
                        {
                            List<Symbol> extents = sqlSelectStatement.FromExtents;
                            JoinSymbol newJoinSymbol = new JoinSymbol(input.VariableName, input.VariableType, extents);
                            newJoinSymbol.IsNestedJoin = true;
                            newJoinSymbol.ColumnList = columns;

                            fromSymbol = newJoinSymbol;
                        }
                        else
                        {
                            // this is a copy of the code in CreateNewSelectStatement.

                            // if the oldStatement has a join as its input, ...
                            // clone the join symbol, so that we "reuse" the
                            // join symbol.  Normally, we create a new symbol - see the next block
                            // of code.
                            JoinSymbol oldJoinSymbol = sqlSelectStatement.FromExtents[0] as JoinSymbol;
                            if (oldJoinSymbol != null)
                            {
                                // Note: sqlSelectStatement.FromExtents will not do, since it might
                                // just be an alias of joinSymbol, and we want an actual JoinSymbol.
                                JoinSymbol newJoinSymbol = new JoinSymbol(input.VariableName, input.VariableType, oldJoinSymbol.ExtentList);
                                // This indicates that the sqlSelectStatement is a blocking scope
                                // i.e. it hides/renames extent columns
                                newJoinSymbol.IsNestedJoin = true;
                                newJoinSymbol.ColumnList = columns;
                                newJoinSymbol.FlattenedExtentList = oldJoinSymbol.FlattenedExtentList;

                                fromSymbol = newJoinSymbol;
                            }
                            else if (sqlSelectStatement.FromExtents[0].OutputColumnsRenamed)
                            {
                                fromSymbol = new Symbol(input.VariableName, input.VariableType, sqlSelectStatement.FromExtents[0].Columns);
                            }
                        }

                    }
                    else if (sqlSelectStatement.OutputColumnsRenamed)
                    {
                        fromSymbol = new Symbol(input.VariableName, input.VariableType, sqlSelectStatement.OutputColumns);
                    }
                    result.From.Append(" (");
                    result.From.Append(sqlSelectStatement);
                    result.From.Append(" )");
                }
                else if (input.Expression is DbScanExpression)
                {
                    result.From.Append(fromExtentFragment);
                }
                else // bracket it
                {
                    WrapNonQueryExtent(result, fromExtentFragment, input.Expression.ExpressionKind);
                }

                if (fromSymbol == null) // i.e. not a join symbol
                {
                    fromSymbol = new Symbol(input.VariableName, input.VariableType);
                }


                AddFromSymbol(result, input.VariableName, fromSymbol);
                result.AllJoinExtents.Add(fromSymbol);
            }
            else // result == fromExtentFragment.  The child extents have been merged into the parent's.
            {
                // we are adding extents to the current sql statement via flattening.
                // We are replacing the child's extents with a single Join symbol.
                // The child's extents are all those following the index fromSymbolStart.
                //
                List<Symbol> extents = new List<Symbol>();

                // We cannot call extents.AddRange, since the is no simple way to
                // get the range of symbols fromSymbolStart..result.FromExtents.Count
                // from result.FromExtents.
                // We copy these symbols to create the JoinSymbol later.
                for (int i = fromSymbolStart; i < result.FromExtents.Count; ++i)
                {
                    extents.Add(result.FromExtents[i]);
                }
                result.FromExtents.RemoveRange(fromSymbolStart, result.FromExtents.Count - fromSymbolStart);
                fromSymbol = new JoinSymbol(input.VariableName, input.VariableType, extents);
                result.FromExtents.Add(fromSymbol);
                // this Join Symbol does not have its own select statement, so we
                // do not set IsNestedJoin


                // We do not call AddFromSymbol(), since we do not want to add
                // "AS alias" to the FROM clause- it has been done when the extent was added earlier.
                symbolTable.Add(input.VariableName, fromSymbol);
            }
        }

        /// <summary>
        /// <see cref="AddDefaultColumns"/>
        /// Add the column names from the referenced extent/join to the
        /// select statement.
        ///
        /// If the symbol is a JoinSymbol, we recursively visit all the extents,
        /// halting at real extents and JoinSymbols that have an associated SqlSelectStatement.
        ///
        /// The column names for a real extent can be derived from its type.
        /// The column names for a Join Select statement can be got from the
        /// list of columns that was created when the Join's select statement
        /// was created.
        ///
        /// We do the following for each column.
        /// <list type="number">
        /// <item>Add the SQL string for each column to the SELECT clause</item>
        /// <item>Add the column to the list of columns - so that it can
        /// become part of the "type" of a JoinSymbol</item>
        /// <item>Check if the column name collides with a previous column added
        /// to the same select statement.  Flag both the columns for renaming if true.</item>
        /// <item>Add the column to a name lookup dictionary for collision detection.</item>
        /// </list>
        /// </summary>
        /// <param name="selectStatement">The select statement that started off as SELECT *</param>
        /// <param name="symbol">The symbol containing the type information for
        /// the columns to be added.</param>
        /// <param name="columnList">Columns that have been added to the Select statement.
        /// This is created in <see cref="AddDefaultColumns"/>.</param>
        /// <param name="columnDictionary">A dictionary of the columns above.</param>
        private void AddColumns(SqlSelectStatement selectStatement, Symbol symbol, List<Symbol> columnList, Dictionary<string, Symbol> columnDictionary)
        {
            var joinSymbol = symbol as JoinSymbol;
            if (joinSymbol != null)
            {
                if (!joinSymbol.IsNestedJoin)
                {
                    // Recurse if the join symbol is a collection of flattened extents
                    foreach (Symbol sym in joinSymbol.ExtentList)
                    {
                        // if sym is ScalarType means we are at base case in the
                        // recursion and there are not columns to add, just skip
                        if ((sym.Type == null) || MetadataHelpers.IsPrimitiveType(sym.Type.EdmType))
                        {
                            continue;
                        }

                        AddColumns(selectStatement, sym, columnList, columnDictionary);
                    }
                }
                else
                {
                    foreach (Symbol joinColumn in joinSymbol.ColumnList)
                    {
                        // we write tableName.columnName
                        // rather than tableName.columnName as alias
                        // since the column name is unique (by the way we generate new column names)
                        //
                        // We use the symbols for both the table and the column,
                        // since they are subject to renaming.
                        selectStatement.Select.Append(new SelectColumn(joinColumn, symbol, joinColumn));

                        // check for name collisions.  If there is,
                        // flag both the colliding symbols.
                        if (columnDictionary.ContainsKey(joinColumn.Name))
                        {
                            columnDictionary[joinColumn.Name].NeedsRenaming = true; // the original symbol
                            joinColumn.NeedsRenaming = true; // the current symbol.
                        }
                        else
                        {
                            columnDictionary[joinColumn.Name] = joinColumn;
                        }

                        columnList.Add(joinColumn);
                    }
                }
            }
            else
            {
                // This is a non-join extent/select statement, and the CQT type has
                // the relevant column information.

                // The type could be a record type(e.g. Project(...),
                // or an entity type ( e.g. EntityExpression(...)
                // so, we check whether it is a structuralType.

                // Consider an expression of the form J(a, b=P(E))
                // The inner P(E) would have been translated to a SQL statement
                // We should not use the raw names from the type, but the equivalent
                // symbols (they are present in symbol.Columns) if they exist.
                //
                // We add the new columns to the symbol's columns if they do
                // not already exist.
                //
                // If the symbol represents a SqlStatement with renamed output columns,
                // we should use these instead of the rawnames and we should also mark
                // this selectStatement as one with renamed columns

                if (symbol.OutputColumnsRenamed)
                {
                    selectStatement.OutputColumnsRenamed = true;
                    selectStatement.OutputColumns = new Dictionary<string, Symbol>();
                }

                if ((symbol.Type == null) || MetadataHelpers.IsPrimitiveType(symbol.Type.EdmType))
                {
                    AddColumn(selectStatement, symbol, columnList, columnDictionary, "X");
                }
                else
                {
                    foreach (EdmProperty property in MetadataHelpers.GetProperties(symbol.Type))
                    {
                        AddColumn(selectStatement, symbol, columnList, columnDictionary, property.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method for AddColumns. Adds a column with the given column name 
        /// to the Select list of the given select statement.
        /// </summary>
        /// <param name="selectStatement">The select statement to whose SELECT part the column should be added</param>
        /// <param name="symbol">The symbol from which the column to be added originated</param>
        /// <param name="columnList">Columns that have been added to the Select statement.
        /// This is created in <see cref="AddDefaultColumns"/>.</param>
        /// <param name="columnDictionary">A dictionary of the columns above.</param>
        /// <param name="columnName">The name of the column to be added.</param>
        private void AddColumn(SqlSelectStatement selectStatement, Symbol symbol, List<Symbol> columnList, Dictionary<string, Symbol> columnDictionary, string columnName)
        {
            // Since all renaming happens in the second phase
            // we lose nothing by setting the next column name index to 0
            // many times.
            allColumnNames[columnName] = 0;

            // Create a new symbol/reuse existing symbol for the column
            Symbol columnSymbol;
            if (!symbol.Columns.TryGetValue(columnName, out columnSymbol))
            {
                // we do not care about the types of columns, so we pass null
                // when construction the symbol.
                columnSymbol = new Symbol(columnName, null);
                symbol.Columns.Add(columnName, columnSymbol);
            }

            if (symbol.OutputColumnsRenamed)
            {
                selectStatement.Select.Append(new SelectColumn(columnSymbol, symbol, columnSymbol));
                selectStatement.OutputColumns.Add(columnSymbol.Name, columnSymbol);
            }

            // We use the actual name before the "AS", the new name goes
            // after the AS.
            else
            {
                selectStatement.Select.Append(new SelectColumn(columnSymbol, symbol, new Symbol(columnName)));
            }

            // Check for column name collisions.
            if (columnDictionary.ContainsKey(columnName))
            {
                columnDictionary[columnName].NeedsRenaming = true;
                columnSymbol.NeedsRenaming = true;
            }
            else
            {
                columnDictionary[columnName] = symbol.Columns[columnName];
            }

            columnList.Add(columnSymbol);
        }

        /// <summary>
        /// Expands Select * to "select the_list_of_columns"
        /// If the columns are taken from an extent, they are written as
        /// {original_column_name AS Symbol(original_column)} to allow renaming.
        ///
        /// If the columns are taken from a Join, they are written as just
        /// {original_column_name}, since there cannot be a name collision.
        ///
        /// We concatenate the columns from each of the inputs to the select statement.
        /// Since the inputs may be joins that are flattened, we need to recurse.
        /// The inputs are inferred from the symbols in FromExtents.
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <returns></returns>
        private List<Symbol> AddDefaultColumns(SqlSelectStatement selectStatement)
        {
            // This is the list of columns added in this select statement
            // This forms the "type" of the Select statement, if it has to
            // be expanded in another SELECT *
            List<Symbol> columnList = new List<Symbol>();

            // A lookup for the previous set of columns to aid column name
            // collision detection.
            Dictionary<string, Symbol> columnDictionary = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);

            foreach (Symbol symbol in selectStatement.FromExtents)
            {
                AddColumns(selectStatement, symbol, columnList, columnDictionary);
            }

            return columnList;
        }

        /// <summary>
        /// <see cref="AddFromSymbol(SqlSelectStatement, string, Symbol, bool)"/>
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <param name="inputVarName"></param>
        /// <param name="fromSymbol"></param>
        private void AddFromSymbol(SqlSelectStatement selectStatement, string inputVarName, Symbol fromSymbol)
        {
            AddFromSymbol(selectStatement, inputVarName, fromSymbol, true);
        }

        /// <summary>
        /// This method is called after the input to a relational node is visited.
        /// <see cref="Visit(DbProjectExpression)"/> and <see cref="ProcessJoinInputResult"/>
        /// There are 2 scenarios
        /// <list type="number">
        /// <item>The fromSymbol is new i.e. the select statement has just been
        /// created, or a join extent has been added.</item>
        /// <item>The fromSymbol is old i.e. we are reusing a select statement.</item>
        /// </list>
        ///
        /// If we are not reusing the select statement, we have to complete the
        /// FROM clause with the alias
        /// <code>
        /// -- if the input was an extent
        /// FROM = [SchemaName].[TableName]
        /// -- if the input was a Project
        /// FROM = (SELECT ... FROM ... WHERE ...)
        /// </code>
        ///
        /// These become
        /// <code>
        /// -- if the input was an extent
        /// FROM = [SchemaName].[TableName] AS alias
        /// -- if the input was a Project
        /// FROM = (SELECT ... FROM ... WHERE ...) AS alias
        /// </code>
        /// and look like valid FROM clauses.
        ///
        /// Finally, we have to add the alias to the global list of aliases used,
        /// and also to the current symbol table.
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <param name="inputVarName">The alias to be used.</param>
        /// <param name="fromSymbol"></param>
        /// <param name="addToSymbolTable"></param>
        private void AddFromSymbol(SqlSelectStatement selectStatement, string inputVarName, Symbol fromSymbol, bool addToSymbolTable)
        {
            // the first check is true if this is a new statement
            // the second check is true if we are in a join - we do not
            // check if we are in a join context.
            // We do not want to add "AS alias" if it has been done already
            // e.g. when we are reusing the Sql statement.
            if (selectStatement.FromExtents.Count == 0 || fromSymbol != selectStatement.FromExtents[0])
            {
                selectStatement.FromExtents.Add(fromSymbol);
                selectStatement.From.Append(" as ");
                selectStatement.From.Append(fromSymbol);

                // We have this inside the if statement, since
                // we only want to add extents that are actually used.
                allExtentNames[fromSymbol.Name] = 0;
            }

            if (addToSymbolTable)
            {
                symbolTable.Add(inputVarName, fromSymbol);
            }
        }

        /// <summary>
        /// If the sql fragment for an input expression is not a SqlSelect statement
        /// or other acceptable form (e.g. an extent as a SqlBuilder), we need
        /// to wrap it in a form acceptable in a FROM clause.  These are
        /// primarily the
        /// <list type="bullet">
        /// <item>The set operation expressions - union all, intersect, except</item>
        /// <item>TVFs, which are conceptually similar to tables</item>
        /// </list>
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sqlFragment"></param>
        /// <param name="expressionKind"></param>
        private static void WrapNonQueryExtent(SqlSelectStatement result, ISqlFragment sqlFragment, DbExpressionKind expressionKind)
        {
            switch (expressionKind)
            {
                case DbExpressionKind.Function:
                    // TVF
                    result.From.Append(sqlFragment);
                    break;

                default:
                    result.From.Append(" (");
                    result.From.Append(sqlFragment);
                    result.From.Append(")");
                    break;
            }
        }

        private ISqlFragment VisitSetOpExpression(DbExpression left, DbExpression right, string separator)
        {
            var leftSelectStatement = VisitExpressionEnsureSqlStatement(left);
            var rightSelectStatement = VisitExpressionEnsureSqlStatement(right);
            return new SqlBuilder(leftSelectStatement, Environment.NewLine, separator, Environment.NewLine, rightSelectStatement);
        }
    }
}
