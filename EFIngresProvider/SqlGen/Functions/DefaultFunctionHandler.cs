using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class DefaultFunctionHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            var result = new SqlBuilder();
            sqlGenerator.WriteFunctionName(result, e.Function);
            HandleFunctionArgumentsDefault(sqlGenerator, e, result);
            return result;
        }
    }
}
