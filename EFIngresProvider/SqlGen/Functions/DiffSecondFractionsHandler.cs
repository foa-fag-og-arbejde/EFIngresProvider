using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class DiffSecondFractionsHandler : FunctionHandler
    {
        public int Factor { get; set; }

        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2);

            var startExpression = e.Arguments[0].Accept(sqlGenerator);
            var endExpression = e.Arguments[1].Accept(sqlGenerator);
            var diff = new SqlBuilder("(" + endExpression, " - ", startExpression + ")");

            return new SqlBuilder(
                "int4((",
                "(",
                "case when left(varchar(", diff, "), 1) = '-' then -1 else 1 end * ",
                "(",
                "decimal(abs(interval('second', ingresdate(", diff, "))), 20, 10)",
                " + ",
                "decimal('0.' + substring(varchar(", diff, ") from locate(varchar(", diff, "), '.') + 1), 20, 10)",
                ")",
                ")",
                ") * ", Factor, ")"
            );
        }
    }
}
