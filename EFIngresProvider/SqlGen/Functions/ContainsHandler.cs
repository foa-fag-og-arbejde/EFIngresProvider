using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    /// <summary>
    /// CONTAINS(arg0, arg1) => arg0 LIKE '%arg1%'
    /// </summary>
    public class ContainsHandler : PatternHandlerBase
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2);

            ISqlFragment result;

            string value;
            if (TryGetConstantString(e.Arguments[1], out value))
            {
                result = GetLikePredicate(sqlGenerator, e.Arguments[0], value, insertPercentStart: true, insertPercentEnd: true);
            }
            else
            {
                // We use LOCATE when the search param is a DbNullExpression or is not a constant string.
                result = new SqlBuilder(
                    "locate(",
                    e.Arguments[0].Accept(sqlGenerator),
                    ", ",
                    e.Arguments[1].Accept(sqlGenerator),
                    ") <= size(",
                    e.Arguments[0].Accept(sqlGenerator),
                    ")"
                );
            }

            return WrapPredicate(result);
        }
    }
}
