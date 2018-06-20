using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class DayOfYearHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 1);
            var expression = e.Arguments[0].Accept(sqlGenerator);
            return new SqlBuilder("int4(interval('day', ingresdate(date_trunc('day', ", expression, ") - date_trunc('year', ", expression, "))) + 1)");
        }
    }
}
