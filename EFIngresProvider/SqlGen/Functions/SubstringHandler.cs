using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class SubstringHandler : FunctionHandler
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 3);

            return new SqlBuilder(
                "substring(",
                e.Arguments[0].Accept(sqlGenerator),
                " from ",
                e.Arguments[1].Accept(sqlGenerator),
                " for ",
                e.Arguments[2].Accept(sqlGenerator),
                ")"
            );
        }
    }
}
