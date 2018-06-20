using System;
using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class NotSupportedHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            throw new NotSupportedException("Function " + e.Function.Name + " is not supported");
        }
    }
}
