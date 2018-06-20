using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class TruncateTimeHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 1);
            return new SqlBuilder("date_trunc('day', ", e.Arguments[0].Accept(sqlGenerator), ")");
        }
    }
}
