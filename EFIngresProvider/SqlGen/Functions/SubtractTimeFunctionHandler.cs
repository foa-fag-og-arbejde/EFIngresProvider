using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class SubtractTimeFunctionHandler : PatternHandlerBase
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2);
            var left = e.Arguments[0].Accept(sqlGenerator);
            var right = e.Arguments[1].Accept(sqlGenerator);
            return new SqlBuilder(left, " - ", right);
        }
    }
}
