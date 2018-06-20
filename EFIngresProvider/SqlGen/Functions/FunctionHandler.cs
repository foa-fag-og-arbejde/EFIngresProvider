using System;
using System.Data.Common.CommandTrees;
using EFIngresProvider.Helpers;
using System.Diagnostics;

namespace EFIngresProvider.SqlGen.Functions
{
    public abstract class FunctionHandler
    {
        public abstract ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e);

        /// <summary>
        /// Default handling on function arguments
        /// Appends the list of arguments to the given result
        /// If the function is niladic it does not append anything,
        /// otherwise it appends (arg1, arg2, ..., argn)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="result"></param>
        protected void HandleFunctionArgumentsDefault(SqlGenerator sqlGenerator, DbFunctionExpression e, SqlBuilder result)
        {
            var isNiladicFunction = MetadataHelpers.TryGetValueForMetadataProperty<bool>(e.Function, "NiladicFunctionAttribute");
            if (isNiladicFunction && e.Arguments.Count > 0)
            {
                throw new InvalidOperationException("Niladic functions cannot have parameters");
            }

            if (!isNiladicFunction)
            {
                result.Append("(");
                var separator = "";
                foreach (var arg in e.Arguments)
                {
                    result.Append(separator, arg.Accept(sqlGenerator));
                    separator = ", ";
                }
                result.Append(")");
            }
        }

        protected void AssertArgumentCount(DbFunctionExpression e, int count)
        {
            Debug.Assert(e.Arguments.Count == count, string.Format("{0} should have {1} argument(s)", e.Function.Name, count));
        }

        protected void AssertArgumentCount(DbFunctionExpression e, int minCount, int maxCount)
        {
            Debug.Assert(e.Arguments.Count >= minCount && e.Arguments.Count <= maxCount, string.Format("{0} should have between {1} and {2} argument(s)", e.Function.Name, minCount, maxCount));
        }
    }
}
