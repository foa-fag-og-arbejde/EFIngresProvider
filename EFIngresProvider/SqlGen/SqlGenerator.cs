using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees;
using System.Diagnostics;
using System.Data.Metadata.Edm;
using EFIngresProvider.Helpers;
using System.Data;
using System.Data.Common;

namespace EFIngresProvider.SqlGen
{
    public partial class SqlGenerator : DbExpressionVisitor<ISqlFragment>
    {
        #region Visitor parameter stacks
        /// <summary>
        /// Every relational node has to pass its SELECT statement to its children
        /// This allows them (DbVariableReferenceExpression eventually) to update the list of
        /// outer extents (free variables) used by this select statement.
        /// </summary>
        private Stack<SqlSelectStatement> selectStatementStack;

        /// <summary>
        /// The top of the stack
        /// </summary>
        private SqlSelectStatement CurrentSelectStatement
        {
            // There is always something on the stack, so we can always Peek.
            get { return selectStatementStack.Peek(); }
        }

        /// <summary>
        /// Nested joins and extents need to know whether they should create
        /// a new Select statement, or reuse the parent's.  This flag
        /// indicates whether the parent is a join or not.
        /// </summary>
        private Stack<bool> isParentAJoinStack;

        /// <summary>
        /// The top of the stack
        /// </summary>
        private bool IsParentAJoin
        {
            // There might be no entry on the stack if a Join node has never
            // been seen, so we return false in that case.
            get { return isParentAJoinStack.Count == 0 ? false : isParentAJoinStack.Peek(); }
        }

        #endregion

        #region Global lists and state
        Dictionary<string, int> allExtentNames;
        internal Dictionary<string, int> AllExtentNames
        {
            get { return allExtentNames; }
        }

        // For each column name, we store the last integer suffix that
        // was added to produce a unique column name.  This speeds up
        // the creation of the next unique name for this column name.
        Dictionary<string, int> allColumnNames;
        internal Dictionary<string, int> AllColumnNames
        {
            get { return allColumnNames; }
        }

        SymbolTable symbolTable = new SymbolTable();

        /// <summary>
        /// VariableReferenceExpressions are allowed only as children of DbPropertyExpression
        /// or MethodExpression.  The cheapest way to ensure this is to set the following
        /// property in DbVariableReferenceExpression and reset it in the allowed parent expressions.
        /// </summary>
        private bool isVarRefSingle;

        private EFIngresStoreVersion storeVersion;

        #endregion

        #region Constructor
        /// <summary>
        /// Basic constructor. 
        /// </summary>
        /// <param name="storeVersion">server version</param>
        private SqlGenerator(EFIngresStoreVersion storeVersion)
        {
            this.storeVersion = storeVersion;
        }
        #endregion

        #region Entry points
        /// <summary>
        /// General purpose static function that can be called from System.Data assembly
        /// </summary>
        /// <param name="tree">command tree</param>
        /// <param name="version">version</param>
        /// <param name="parameters">Parameters to add to the command tree corresponding
        /// to constants in the command tree. Used only in ModificationCommandTrees.</param>
        /// <returns>The string representing the SQL to be executed.</returns>
        internal static string GenerateSql(DbCommandTree tree, EFIngresStoreVersion version, out List<DbParameter> parameters, out CommandType commandType)
        {
            commandType = CommandType.Text;

            //Handle Query
            DbQueryCommandTree queryCommandTree = tree as DbQueryCommandTree;
            if (queryCommandTree != null)
            {
                SqlGenerator sqlGen = new SqlGenerator(version);
                parameters = null;
                return sqlGen.GenerateSql((DbQueryCommandTree)tree);
            }

            //Handle Function
            DbFunctionCommandTree DbFunctionCommandTree = tree as DbFunctionCommandTree;
            if (DbFunctionCommandTree != null)
            {
                SqlGenerator sqlGen = new SqlGenerator(version);
                parameters = null;

                string sql = sqlGen.GenerateFunctionSql(DbFunctionCommandTree, out commandType);

                return sql;
            }

            //Handle Insert
            DbInsertCommandTree insertCommandTree = tree as DbInsertCommandTree;
            if (insertCommandTree != null)
            {
                return DmlSqlGenerator.GenerateInsertSql(insertCommandTree, out parameters);
            }

            //Handle Delete
            DbDeleteCommandTree deleteCommandTree = tree as DbDeleteCommandTree;
            if (deleteCommandTree != null)
            {
                return DmlSqlGenerator.GenerateDeleteSql(deleteCommandTree, out parameters);
            }

            //Handle Update
            DbUpdateCommandTree updateCommandTree = tree as DbUpdateCommandTree;
            if (updateCommandTree != null)
            {
                return DmlSqlGenerator.GenerateUpdateSql(updateCommandTree, out parameters);
            }

            throw new NotSupportedException("Unrecognized command tree type");
        }
        #endregion

        #region Driver Methods
        /// <summary>
        /// Translate a command tree to a SQL string.
        ///
        /// The input tree could be translated to either a SQL SELECT statement
        /// or a SELECT expression.  This choice is made based on the return type
        /// of the expression
        /// CollectionType => select statement
        /// non collection type => select expression
        /// </summary>
        /// <param name="tree"></param>
        /// <returns>The string representing the SQL to be executed.</returns>
        private string GenerateSql(DbQueryCommandTree tree)
        {
            DbExpression targetExpression = tree.Query;

            selectStatementStack = new Stack<SqlSelectStatement>();
            isParentAJoinStack = new Stack<bool>();

            allExtentNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            allColumnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Literals will not be converted to parameters.

            ISqlFragment result;
            if (MetadataHelpers.IsCollectionType(targetExpression.ResultType))
            {
                SqlSelectStatement sqlStatement = VisitExpressionEnsureSqlStatement(targetExpression);
                Debug.Assert(sqlStatement != null, "The outer most sql statment is null");
                sqlStatement.IsTopMost = true;
                result = sqlStatement;
            }
            else
            {
                SqlBuilder sqlBuilder = new SqlBuilder();
                sqlBuilder.Append("SELECT ");
                sqlBuilder.Append(targetExpression.Accept(this));

                result = sqlBuilder;
            }

            if (isVarRefSingle)
            {
                throw new NotSupportedException();
                // A DbVariableReferenceExpression has to be a child of DbPropertyExpression or MethodExpression
            }

            // Check that the parameter stacks are not leaking.
            Debug.Assert(selectStatementStack.Count == 0);
            Debug.Assert(isParentAJoinStack.Count == 0);

            return WriteSql(result);
        }

        /// <summary>
        /// Translate a function command tree to a SQL string.
        /// </summary>
        private string GenerateFunctionSql(DbFunctionCommandTree tree, out CommandType commandType)
        {
            EdmFunction function = tree.EdmFunction;

            // We expect function to always have these properties
            string userCommandText = (string)function.MetadataProperties["CommandTextAttribute"].Value;
            string userSchemaName = (string)function.MetadataProperties["Schema"].Value;
            string userFuncName = (string)function.MetadataProperties["StoreFunctionNameAttribute"].Value;

            if (String.IsNullOrEmpty(userCommandText))
            {
                // build a quoted description of the function
                commandType = CommandType.StoredProcedure;

                // if the schema name is not explicitly given, it is assumed to be the metadata namespace
                string schemaName = String.IsNullOrEmpty(userSchemaName) ?
                    function.NamespaceName : userSchemaName;

                // if the function store name is not explicitly given, it is assumed to be the metadata name
                string functionName = String.IsNullOrEmpty(userFuncName) ?
                    function.Name : userFuncName;

                // quote elements of function text
                string quotedSchemaName = QuoteIdentifier(schemaName);
                string quotedFunctionName = QuoteIdentifier(functionName);

                // separator
                const string schemaSeparator = ".";

                // concatenate elements of function text
                string quotedFunctionText = quotedSchemaName + schemaSeparator + quotedFunctionName;

                return quotedFunctionText;
            }
            else
            {
                // if the user has specified the command text, pass it through verbatim and choose CommandType.Text
                commandType = CommandType.Text;
                return userCommandText;
            }
        }

        /// <summary>
        /// Convert the SQL fragments to a string.
        /// We have to setup the Stream for writing.
        /// </summary>
        /// <param name="sqlStatement"></param>
        /// <returns>A string representing the SQL to be executed.</returns>
        private string WriteSql(ISqlFragment sqlStatement)
        {
            using (SqlWriter writer = new SqlWriter())
            {
                sqlStatement.WriteSql(writer, this);
                return writer.ToString();
            }
        }

        #endregion
    }
}
