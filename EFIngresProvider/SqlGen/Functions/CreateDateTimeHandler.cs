using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class CreateDateTimeHandler : CreateDateTimeBaseHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 6);

            var year = e.Arguments[0].Accept(sqlGenerator);
            var month = e.Arguments[1].Accept(sqlGenerator);
            var day = e.Arguments[2].Accept(sqlGenerator);
            var hour = e.Arguments[3].Accept(sqlGenerator);
            var minute = e.Arguments[4].Accept(sqlGenerator);
            var second = e.Arguments[5].Accept(sqlGenerator);

            return new SqlBuilder("timestamp(", Date(year, month, day), " + ' ' + ", Time(hour, minute, second), ")");
        }
    }
}
