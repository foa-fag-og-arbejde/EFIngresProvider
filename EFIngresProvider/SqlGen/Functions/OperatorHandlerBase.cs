using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    /// <summary>
    /// Handles functions that are translated into SQL operators.
    /// The given function should have one or two arguments. 
    /// Functions with one arguemnt are translated into 
    ///     op arg
    /// Functions with two arguments are translated into
    ///     arg0 op arg1
    /// Also, the arguments can be optionaly enclosed in parethesis
    /// </summary>
    public abstract class OperatorHandlerBase : FunctionHandler
    {
        /// <summary>
        /// The SQL operator
        /// </summary>
        public abstract string Operator { get; }

        /// <summary>
        /// Whether the arguments should be enclosed in parethesis
        /// </summary>
        public abstract bool ParenthesiseArguments { get; }

        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 1, 2);
            SqlBuilder result = new SqlBuilder();

            if (e.Arguments.Count > 1)
            {
                if (ParenthesiseArguments)
                {
                    result.Append("(");
                }
                result.Append(e.Arguments[0].Accept(sqlGenerator));
                if (ParenthesiseArguments)
                {
                    result.Append(")");
                }
            }
            result.Append(" ", Operator, " ");

            if (ParenthesiseArguments)
            {
                result.Append("(");
            }
            result.Append(e.Arguments[e.Arguments.Count - 1].Accept(sqlGenerator));
            if (ParenthesiseArguments)
            {
                result.Append(")");
            }
            return result;
        }
    }
}
