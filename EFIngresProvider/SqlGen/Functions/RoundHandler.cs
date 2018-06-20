using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class RoundHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 1, 2);

            var value = e.Arguments[0].Accept(sqlGenerator);
            object digits = "0";
            if (e.Arguments.Count == 2)
            {
                digits = e.Arguments[1].Accept(sqlGenerator);
            }

            return new SqlBuilder("round(", value, ", ", digits, ")");
        }
    }
}
