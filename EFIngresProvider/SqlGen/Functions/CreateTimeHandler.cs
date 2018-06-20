using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class CreateTimeHandler : CreateDateTimeBaseHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 3);

            var hour = e.Arguments[0].Accept(sqlGenerator);
            var minute = e.Arguments[1].Accept(sqlGenerator);
            var second = e.Arguments[2].Accept(sqlGenerator);

            return new SqlBuilder("time(", Time(hour, minute, second), ")");
        }
    }
}
