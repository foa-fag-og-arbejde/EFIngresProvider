using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public abstract class AddDateTimeHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2);
            var expression = e.Arguments[0].Accept(sqlGenerator);
            var interval = CreateInterval(new SqlBuilder("int4(", e.Arguments[1].Accept(sqlGenerator), ")"));
            return new SqlBuilder("( ", expression, " + ", interval, " )");
        }

        protected abstract IntervalBase CreateInterval(ISqlFragment number);
    }
}
