using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class DiffTimeHandler : FunctionHandler
    {
        public string Unit { get; set; }

        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2);
            var startExpression = e.Arguments[0].Accept(sqlGenerator);
            var endExpression = e.Arguments[1].Accept(sqlGenerator);
            return new SqlBuilder("int4(interval('", Unit, "', ingresdate(", endExpression, " - ", startExpression, ")))");
        }
    }
}
