using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class IndexOfHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2);
            var target = e.Arguments[0].Accept(sqlGenerator);
            var str = e.Arguments[1].Accept(sqlGenerator);
            var locate = new SqlBuilder("locate(", str, ", ", target, ")");
            return new SqlBuilder("case when ", locate, " <= size(", str, ") then int4(", locate, ") else int4(0) end");
        }
    }
}
